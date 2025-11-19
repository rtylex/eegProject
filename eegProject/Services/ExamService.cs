using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using eegProject.Models;

namespace eegProject.Services
{
    internal sealed class ExamService
    {
        /// <summary>
        /// Yeni sınav sonucu oluşturur
        /// </summary>
        public async Task<SinavSonucu> CreateAsync(SinavSonucu result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            using (var context = DbContextFactory.Create())
            {
                context.SinavSonucu.Add(result);
                await context.SaveChangesAsync();
                return result;
            }
        }

        /// <summary>
        /// SinavCevapStatistics'ten SinavSonucu oluşturur
        /// </summary>
        public async Task<SinavSonucu> CreateFromStatisticsAsync(
            int oturumId,
            int? atamaId,
            string sinavTuru,
            SinavCevapStatistics stats,
            DateTime baslamaTarihi,
            DateTime? bitisTarihi = null,
            bool analizeEkle = true)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            var endTime = bitisTarihi ?? DateTime.UtcNow;
            var duration = (int)(endTime - baslamaTarihi).TotalMinutes;

            var result = new SinavSonucu
            {
                OturumID = oturumId,
                AtamaID = atamaId,
                SinavTuru = sinavTuru,
                ToplamSoru = stats.ToplamSoru,
                DogruSayisi = stats.DogruSayisi,
                YanlisSayisi = stats.YanlisSayisi,
                BaslamaTarihi = baslamaTarihi,
                BitisTarihi = endTime,
                Sure = duration.ToString() + " dakika",
                OrtalamaCevapSuresi = stats.OrtalamaCevapSuresi,
                ToplamPuan = stats.ToplamPuan,
                AlinanPuan = stats.AlinanPuan,
                BasariYuzdesi = stats.BasariYuzdesi,
                CokSeçmeliSayisi = stats.CokSeçmeliSayisi,
                KlasikSoruSayisi = stats.KlasikSayisi,
                AnalizeEkle = analizeEkle
            };

            return await CreateAsync(result);
        }

        public async Task<List<SinavSonucu>> GetBySessionAsync(int sessionId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavSonucu
                    .Where(s => s.OturumID == sessionId)
                    .OrderByDescending(s => s.BaslamaTarihi)
                    .ToListAsync();
            }
        }

        public async Task<List<SinavSonucu>> GetByUserAsync(int userId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavSonucu
                    .Include(s => s.Oturum)
                    .Where(s => s.Oturum.KullaniciID == userId)
                    .OrderByDescending(s => s.BaslamaTarihi)
                    .ToListAsync();
            }
        }

        public async Task DeleteAsync(int examResultId)
        {
            using (var context = DbContextFactory.Create())
            {
                var result = await context.SinavSonucu
                    .FirstOrDefaultAsync(s => s.SinavSonucuID == examResultId);
                if (result == null) return;

                context.SinavSonucu.Remove(result);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Oturuma ait sınav sonuçlarını AI analiz formatında hazırlar
        /// </summary>
        public async Task<Dictionary<string, object>> GetExamDataForAnalysisAsync(int sessionId)
        {
            using (var context = DbContextFactory.Create())
            {
                var sinavSonuclari = await context.SinavSonucu
                    .Where(s => s.OturumID == sessionId && s.AnalizeEkle)
                    .ToListAsync();

                if (sinavSonuclari.Count == 0)
                    return null;

                // Birden fazla sınav varsa hepsini topla
                var toplamSoru = sinavSonuclari.Sum(s => s.ToplamSoru);
                var toplamDogru = sinavSonuclari.Sum(s => s.DogruSayisi);
                var toplamYanlis = sinavSonuclari.Sum(s => s.YanlisSayisi);
                var toplamPuan = sinavSonuclari.Sum(s => s.ToplamPuan ?? 0);
                var alinanPuan = sinavSonuclari.Sum(s => s.AlinanPuan ?? 0);
                var ortCevapSuresi = sinavSonuclari.Average(s => s.OrtalamaCevapSuresi ?? 0);

                return new Dictionary<string, object>
                {
                    ["exam_count"] = sinavSonuclari.Count,
                    ["exam_types"] = string.Join(", ", sinavSonuclari.Select(s => s.SinavTuru).Distinct()),
                    ["total_questions"] = toplamSoru,
                    ["correct"] = toplamDogru,
                    ["wrong"] = toplamYanlis,
                    ["success_rate"] = toplamSoru > 0 ? (toplamDogru * 100.0 / toplamSoru) : 0,
                    ["total_score"] = toplamPuan,
                    ["obtained_score"] = alinanPuan,
                    ["score_percentage"] = toplamPuan > 0 ? (alinanPuan * 100.0 / toplamPuan) : 0,
                    ["average_answer_time"] = ortCevapSuresi,
                    ["multiple_choice_count"] = sinavSonuclari.Sum(s => s.CokSeçmeliSayisi ?? 0),
                    ["classic_question_count"] = sinavSonuclari.Sum(s => s.KlasikSoruSayisi ?? 0)
                };
            }
        }

        /// <summary>
        /// SinavSonucu'yu günceller (AnalizeEkle flag'i için)
        /// </summary>
        public async Task UpdateAnalyzeIncludeFlagAsync(int sinavSonucuId, bool analizeEkle)
        {
            using (var context = DbContextFactory.Create())
            {
                var result = await context.SinavSonucu.FindAsync(sinavSonucuId);
                if (result == null) return;

                result.AnalizeEkle = analizeEkle;
                await context.SaveChangesAsync();
            }
        }
    }
}



