using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace eegProject.Services
{
    /// <summary>
    /// Sınav cevapları servisi - Soru bazlı detaylı kayıt
    /// </summary>
    internal sealed class SinavCevapService
    {
        /// <summary>
        /// Tek bir soru cevabını kaydeder
        /// </summary>
        public async Task<SinavCevap> CreateAsync(
            int sinavSonucuId,
            int soruNo,
            string soruTipi,
            string soruMetni,
            string dogruCevap,
            string verilenCevap,
            int? cevaplamaSuresi,
            double? toplamPuan = null,
            List<string> anahtarKelimeler = null)
        {
            using (var context = DbContextFactory.Create())
            {
                bool dogruMu = false;
                double? alinanPuan = null;
                double? eslesmeYuzdesi = null;
                string eslesenAnahtarKelimeler = null;

                // Cevap değerlendirmesi
                if (soruTipi == "Klasik" && anahtarKelimeler != null && anahtarKelimeler.Count > 0)
                {
                    // Anahtar kelime eşleştirmesi
                    var eslesenler = EvaluateClassicAnswer(verilenCevap, anahtarKelimeler);
                    eslesenAnahtarKelimeler = JsonConvert.SerializeObject(eslesenler);
                    eslesmeYuzdesi = (eslesenler.Count * 100.0) / anahtarKelimeler.Count;
                    
                    if (toplamPuan.HasValue)
                    {
                        alinanPuan = (eslesenler.Count * toplamPuan.Value) / anahtarKelimeler.Count;
                    }
                    
                    // Eğer %50'den fazla eşleşme varsa doğru kabul et
                    dogruMu = eslesmeYuzdesi >= 50;
                }
                else
                {
                    // Çoktan seçmeli veya Doğru-Yanlış
                    dogruMu = string.Equals(dogruCevap, verilenCevap, StringComparison.OrdinalIgnoreCase);
                    
                    if (dogruMu && toplamPuan.HasValue)
                    {
                        alinanPuan = toplamPuan.Value;
                    }
                }

                var cevap = new SinavCevap
                {
                    SinavSonucuID = sinavSonucuId,
                    SoruNo = soruNo,
                    SoruTipi = soruTipi,
                    SoruMetni = soruMetni,
                    DogruCevap = dogruCevap,
                    VerilenCevap = verilenCevap,
                    CevaplamaSuresi = cevaplamaSuresi,
                    DogruMu = dogruMu,
                    ToplamPuan = toplamPuan,
                    AlinanPuan = alinanPuan,
                    AnahtarKelimelerJson = anahtarKelimeler != null ? JsonConvert.SerializeObject(anahtarKelimeler) : null,
                    EslesenAnahtarKelimeler = eslesenAnahtarKelimeler,
                    EslesmeYuzdesi = eslesmeYuzdesi,
                    CevapTarihi = DateTime.UtcNow
                };

                context.SinavCevap.Add(cevap);
                await context.SaveChangesAsync();
                return cevap;
            }
        }

        /// <summary>
        /// Klasik soru cevabını anahtar kelimelerle değerlendirir
        /// </summary>
        private List<string> EvaluateClassicAnswer(string userAnswer, List<string> keywords)
        {
            if (string.IsNullOrWhiteSpace(userAnswer) || keywords == null || keywords.Count == 0)
                return new List<string>();

            var matchedKeywords = new List<string>();
            var normalizedAnswer = NormalizeText(userAnswer);

            foreach (var keyword in keywords)
            {
                var normalizedKeyword = NormalizeText(keyword);
                
                // Basit substring matching
                if (normalizedAnswer.Contains(normalizedKeyword))
                {
                    matchedKeywords.Add(keyword);
                }
            }

            return matchedKeywords;
        }

        /// <summary>
        /// Metni normalize eder (küçük harf, Türkçe karakter düzeltme)
        /// </summary>
        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text.ToLowerInvariant()
                .Replace('ı', 'i')
                .Replace('ğ', 'g')
                .Replace('ü', 'u')
                .Replace('ş', 's')
                .Replace('ö', 'o')
                .Replace('ç', 'c')
                .Trim();
        }

        /// <summary>
        /// Sınav sonucuna ait tüm cevapları getirir
        /// </summary>
        public async Task<List<SinavCevap>> GetByExamResultAsync(int sinavSonucuId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavCevap
                    .Where(c => c.SinavSonucuID == sinavSonucuId)
                    .OrderBy(c => c.SoruNo)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Toplu cevap kaydı
        /// </summary>
        public async Task<List<SinavCevap>> CreateBulkAsync(List<SinavCevap> cevaplar)
        {
            using (var context = DbContextFactory.Create())
            {
                context.SinavCevap.AddRange(cevaplar);
                await context.SaveChangesAsync();
                return cevaplar;
            }
        }

        /// <summary>
        /// Belirli bir soru tipine göre cevapları getirir
        /// </summary>
        public async Task<List<SinavCevap>> GetByQuestionTypeAsync(int sinavSonucuId, string soruTipi)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavCevap
                    .Where(c => c.SinavSonucuID == sinavSonucuId && c.SoruTipi == soruTipi)
                    .OrderBy(c => c.SoruNo)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Doğru cevapların sayısını döner
        /// </summary>
        public async Task<int> GetCorrectCountAsync(int sinavSonucuId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavCevap
                    .CountAsync(c => c.SinavSonucuID == sinavSonucuId && c.DogruMu);
            }
        }

        /// <summary>
        /// Yanlış cevapların sayısını döner
        /// </summary>
        public async Task<int> GetWrongCountAsync(int sinavSonucuId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavCevap
                    .CountAsync(c => c.SinavSonucuID == sinavSonucuId && !c.DogruMu);
            }
        }

        /// <summary>
        /// Toplam alınan puanı hesaplar
        /// </summary>
        public async Task<double> GetTotalScoreAsync(int sinavSonucuId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavCevap
                    .Where(c => c.SinavSonucuID == sinavSonucuId && c.AlinanPuan.HasValue)
                    .SumAsync(c => c.AlinanPuan.Value);
            }
        }

        /// <summary>
        /// Ortalama cevaplanma süresini hesaplar
        /// </summary>
        public async Task<double> GetAverageAnswerTimeAsync(int sinavSonucuId)
        {
            using (var context = DbContextFactory.Create())
            {
                var cevaplar = await context.SinavCevap
                    .Where(c => c.SinavSonucuID == sinavSonucuId && c.CevaplamaSuresi.HasValue)
                    .ToListAsync();

                if (cevaplar.Count == 0)
                    return 0;

                return cevaplar.Average(c => c.CevaplamaSuresi.Value);
            }
        }

        /// <summary>
        /// Sınav cevaplarını siler
        /// </summary>
        public async Task DeleteByExamResultAsync(int sinavSonucuId)
        {
            using (var context = DbContextFactory.Create())
            {
                var cevaplar = await context.SinavCevap
                    .Where(c => c.SinavSonucuID == sinavSonucuId)
                    .ToListAsync();

                context.SinavCevap.RemoveRange(cevaplar);
                await context.SaveChangesAsync();
            }
        }
    }
}
