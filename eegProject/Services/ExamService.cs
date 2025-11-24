using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using eegProject.Models;
using Newtonsoft.Json;

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
                
                // Süre limitlerini parse et (ilk sınavdan)
                object timeLimits = null;
                var firstExam = sinavSonuclari.FirstOrDefault();
                if (firstExam != null && !string.IsNullOrWhiteSpace(firstExam.JsonDetay))
                {
                    try
                    {
                        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(firstExam.JsonDetay);
                        if (jsonData != null && jsonData.ContainsKey("time_limits"))
                        {
                            timeLimits = jsonData["time_limits"];
                        }
                    }
                    catch { /* JsonDetay parse hatası - görmezden gel */ }
                }
                
                // Gerçek süreyi hesapla (Sure string'inden)
                double? actualDurationMinutes = null;
                if (firstExam != null && !string.IsNullOrWhiteSpace(firstExam.Sure))
                {
                    var sureStr = firstExam.Sure.Replace(" dakika", "").Trim();
                    if (double.TryParse(sureStr, out var mins))
                        actualDurationMinutes = mins;
                }

                var result = new Dictionary<string, object>
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
                    ["true_false_count"] = sinavSonuclari.Sum(s => s.DogruYanlisSayisi ?? 0),
                    ["classic_question_count"] = sinavSonuclari.Sum(s => s.KlasikSoruSayisi ?? 0)
                };
                
                // Süre bilgilerini ekle (varsa)
                if (timeLimits != null)
                {
                    result["time_limits"] = timeLimits;
                }
                
                if (actualDurationMinutes.HasValue)
                {
                    result["actual_duration_minutes"] = actualDurationMinutes.Value;
                }
                
                // ✅ YENİ: SORU BAZLI DETAYLARI EKLE
                var questionDetails = await GetQuestionDetailsForAnalysisAsync(context, sinavSonuclari, timeLimits);
                if (questionDetails != null && questionDetails.Count > 0)
                {
                    result["question_details"] = questionDetails;
                }
                
                return result;
            }
        }

        /// <summary>
        /// Sınav sonuçlarına ait soru detaylarını AI analiz formatında hazırlar (HELPER)
        /// </summary>
        private async Task<List<Dictionary<string, object>>> GetQuestionDetailsForAnalysisAsync(
            eegDBEntities context, 
            List<SinavSonucu> sinavSonuclari,
            object timeLimits)
        {
            var allQuestionDetails = new List<Dictionary<string, object>>();
            
            // Süre limitlerini parse et
            Dictionary<string, int?> limits = null;
            if (timeLimits != null)
            {
                try
                {
                    var limitsJson = JsonConvert.SerializeObject(timeLimits);
                    var limitsDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(limitsJson);
                    
                    limits = new Dictionary<string, int?>
                    {
                        ["multiple_choice"] = limitsDict.ContainsKey("multiple_choice_seconds_per_question") 
                            ? Convert.ToInt32(limitsDict["multiple_choice_seconds_per_question"]) 
                            : (int?)null,
                        ["true_false"] = limitsDict.ContainsKey("true_false_seconds_per_question") 
                            ? Convert.ToInt32(limitsDict["true_false_seconds_per_question"]) 
                            : (int?)null,
                        ["classic"] = limitsDict.ContainsKey("classic_seconds_per_question") 
                            ? Convert.ToInt32(limitsDict["classic_seconds_per_question"]) 
                            : (int?)null
                    };
                }
                catch { /* Parse hatası - limits null kalır */ }
            }

            // Her sınav sonucu için cevapları al
            foreach (var sinav in sinavSonuclari)
            {
                var cevaplar = await context.SinavCevap
                    .Where(c => c.SinavSonucuID == sinav.SinavSonucuID)
                    .OrderBy(c => c.SoruNo)
                    .ToListAsync();

                foreach (var cevap in cevaplar)
                {
                    var questionDetail = new Dictionary<string, object>
                    {
                        ["exam_type"] = sinav.SinavTuru,
                        ["question_no"] = cevap.SoruNo,
                        ["question_type"] = cevap.SoruTipi,
                        ["correct"] = cevap.DogruMu,
                        ["obtained_score"] = cevap.AlinanPuan ?? 0,
                        ["total_score"] = cevap.ToplamPuan ?? 0
                    };

                    // Cevaplama süresi
                    if (cevap.CevaplamaSuresi.HasValue)
                    {
                        questionDetail["answer_time_seconds"] = cevap.CevaplamaSuresi.Value;
                        
                        // Süre limiti ve aşım kontrolü
                        if (limits != null)
                        {
                            int? timeLimit = null;
                            if (cevap.SoruTipi == "CokSeçmeli" && limits.ContainsKey("multiple_choice"))
                                timeLimit = limits["multiple_choice"];
                            else if (cevap.SoruTipi == "DogruYanlis" && limits.ContainsKey("true_false"))
                                timeLimit = limits["true_false"];
                            else if (cevap.SoruTipi == "Klasik" && limits.ContainsKey("classic"))
                                timeLimit = limits["classic"];
                            
                            if (timeLimit.HasValue)
                            {
                                questionDetail["time_limit_seconds"] = timeLimit.Value;
                                questionDetail["time_exceeded"] = cevap.CevaplamaSuresi.Value > timeLimit.Value;
                            }
                        }
                    }

                    // Klasik soru için anahtar kelime bilgisi
                    if (cevap.SoruTipi == "Klasik")
                    {
                        if (cevap.EslesmeYuzdesi.HasValue)
                        {
                            questionDetail["matching_percentage"] = Math.Round(cevap.EslesmeYuzdesi.Value, 1);
                        }
                        
                        // Anahtar kelimeleri parse et
                        if (!string.IsNullOrWhiteSpace(cevap.AnahtarKelimelerJson))
                        {
                            try
                            {
                                var keywords = JsonConvert.DeserializeObject<List<string>>(cevap.AnahtarKelimelerJson);
                                questionDetail["total_keywords"] = keywords?.Count ?? 0;
                            }
                            catch { /* Parse hatası */ }
                        }
                        
                        if (!string.IsNullOrWhiteSpace(cevap.EslesenAnahtarKelimeler))
                        {
                            try
                            {
                                var matched = JsonConvert.DeserializeObject<List<string>>(cevap.EslesenAnahtarKelimeler);
                                questionDetail["matched_keywords"] = matched;
                                questionDetail["matched_keywords_count"] = matched?.Count ?? 0;
                            }
                            catch { /* Parse hatası */ }
                        }
                    }

                    allQuestionDetails.Add(questionDetail);
                }
            }

            return allQuestionDetails;
        }

        /// <summary>
        /// Birden fazla oturuma ait sınav sonuçlarını AI analiz formatında hazırlar (Batch Comparison için)
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetExamDataForAnalysisAsync(List<int> sessionIds)
        {
            if (sessionIds == null || sessionIds.Count == 0)
                return null;

            var result = new List<Dictionary<string, object>>();

            using (var context = DbContextFactory.Create())
            {
                foreach (var sessionId in sessionIds)
                {
                    var sinavSonuclari = await context.SinavSonucu
                        .Include(s => s.Oturum)
                        .Where(s => s.OturumID == sessionId && s.AnalizeEkle)
                        .ToListAsync();

                    if (sinavSonuclari.Count == 0)
                        continue;

                    // Birden fazla sınav varsa hepsini topla
                    var toplamSoru = sinavSonuclari.Sum(s => s.ToplamSoru);
                    var toplamDogru = sinavSonuclari.Sum(s => s.DogruSayisi);
                    var toplamYanlis = sinavSonuclari.Sum(s => s.YanlisSayisi);
                    var toplamPuan = sinavSonuclari.Sum(s => s.ToplamPuan ?? 0);
                    var alinanPuan = sinavSonuclari.Sum(s => s.AlinanPuan ?? 0);
                    var ortCevapSuresi = sinavSonuclari.Average(s => s.OrtalamaCevapSuresi ?? 0);

                    // Süre limitlerini parse et (ilk sınavdan)
                    object timeLimits = null;
                    var firstExam = sinavSonuclari.FirstOrDefault();
                    if (firstExam != null && !string.IsNullOrWhiteSpace(firstExam.JsonDetay))
                    {
                        try
                        {
                            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(firstExam.JsonDetay);
                            if (jsonData != null && jsonData.ContainsKey("time_limits"))
                            {
                                timeLimits = jsonData["time_limits"];
                            }
                        }
                        catch { /* JsonDetay parse hatası - görmezden gel */ }
                    }
                    
                    // Gerçek süreyi hesapla
                    double? actualDurationMinutes = null;
                    if (firstExam != null && !string.IsNullOrWhiteSpace(firstExam.Sure))
                    {
                        var sureStr = firstExam.Sure.Replace(" dakika", "").Trim();
                        if (double.TryParse(sureStr, out var mins))
                            actualDurationMinutes = mins;
                    }
                    
                    var sessionInfo = sinavSonuclari.First().Oturum;
                    var sessionData = new Dictionary<string, object>
                    {
                        ["session_id"] = sessionId,
                        ["time_label"] = sessionInfo?.ZamanEtiketi ?? "Etiketsiz",
                        ["experiment_type"] = sessionInfo?.DeneyTuru ?? "Genel",
                        ["exam_count"] = sinavSonuclari.Count,
                        ["exam_types"] = string.Join(", ", sinavSonuclari.Select(s => s.SinavTuru).Distinct()),
                        ["total_questions"] = toplamSoru,
                        ["correct"] = toplamDogru,
                        ["wrong"] = toplamYanlis,
                        ["success_rate"] = toplamSoru > 0 ? Math.Round(toplamDogru * 100.0 / toplamSoru, 1) : 0,
                        ["total_score"] = toplamPuan,
                        ["obtained_score"] = alinanPuan,
                        ["score_percentage"] = toplamPuan > 0 ? Math.Round(alinanPuan * 100.0 / toplamPuan, 1) : 0,
                        ["average_answer_time_seconds"] = Math.Round(ortCevapSuresi, 1),
                        ["multiple_choice_count"] = sinavSonuclari.Sum(s => s.CokSeçmeliSayisi ?? 0),
                        ["true_false_count"] = sinavSonuclari.Sum(s => s.DogruYanlisSayisi ?? 0),
                        ["classic_question_count"] = sinavSonuclari.Sum(s => s.KlasikSoruSayisi ?? 0)
                    };
                    
                    // Süre bilgilerini ekle (varsa)
                    if (timeLimits != null)
                    {
                        sessionData["time_limits"] = timeLimits;
                    }
                    
                    if (actualDurationMinutes.HasValue)
                    {
                        sessionData["actual_duration_minutes"] = actualDurationMinutes.Value;
                    }
                    
                    // ✅ YENİ: SORU BAZLI DETAYLARI EKLE
                    var questionDetails = await GetQuestionDetailsForAnalysisAsync(context, sinavSonuclari, timeLimits);
                    if (questionDetails != null && questionDetails.Count > 0)
                    {
                        sessionData["question_details"] = questionDetails;
                    }

                    result.Add(sessionData);
                }
            }

            return result.Count > 0 ? result : null;
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

        /// <summary>
        /// Belirli bir kullanıcının tüm sınav sonuçlarını oturum bilgileriyle birlikte getirir
        /// </summary>
        public async Task<List<SinavSonucu>> GetByUserWithSessionsAsync(int userId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavSonucu
                    .Include(s => s.Oturum)
                    .Include(s => s.Oturum.Kullanici)
                    .Where(s => s.Oturum.KullaniciID == userId)
                    .OrderByDescending(s => s.BaslamaTarihi)
                    .ToListAsync();
            }
        }
    }
}



