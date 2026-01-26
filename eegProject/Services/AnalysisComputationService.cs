using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace eegProject.Services
{
    /// <summary>
    /// EEG analiz hesaplamaları için servis
    /// </summary>
    internal sealed class AnalysisComputationService
    {
        private readonly EegDataService _eegDataService = new EegDataService();
        private readonly SessionService _sessionService = new SessionService();
        private readonly ExamService _examService = new ExamService();
        private readonly AiAnalysisService _aiAnalysisService;
        
        private const int MinimumSamplesForRelaxation = 20;
        private const int MinimumSamplesForAttention = 20;
        private const int MinimumSamplesForEngagement = 30;

        public AnalysisComputationService()
        {
            // AI servisini initialize et (API key yoksa null kalır, AI devre dışı)
            try
            {
                _aiAnalysisService = new AiAnalysisService();
            }
            catch
            {
                // API key yoksa veya hatalıysa, AI olmadan devam et
                _aiAnalysisService = null;
            }
        }

        /// <summary>
        /// AI özelliğinin aktif olup olmadığını kontrol eder
        /// </summary>
        public bool IsAiAvailable => _aiAnalysisService != null;

        /// <summary>
        /// Rahatlama analizi hesaplar (Alpha/Beta ratio)
        /// </summary>
        public async Task<AnalizSonucu> ComputeRahatlamaAnaliziAsync(int sessionId, bool useAI = false)
        {
            var session = await GetSessionInfoAsync(sessionId);
            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);

            if (eegData.Count < MinimumSamplesForRelaxation)
            {
                throw new InvalidOperationException($"Rahatlama analizi için en az {MinimumSamplesForRelaxation} sample gereklidir. Mevcut: {eegData.Count}");
            }

            // Ortalama hesapla
            var avgLowAlpha = eegData.Average(e => e.LowAlpha ?? 0);
            var avgHighAlpha = eegData.Average(e => e.HighAlpha ?? 0);
            var avgLowBeta = eegData.Average(e => e.LowBeta ?? 0);
            var avgHighBeta = eegData.Average(e => e.HighBeta ?? 0);

            var ortalamaAlpha = (avgLowAlpha + avgHighAlpha) / 2.0;
            var ortalamaBeta = (avgLowBeta + avgHighBeta) / 2.0;

            // Rahatlama indeksi hesapla
            var rahatlamaIndeksi = ortalamaBeta > 0 ? ortalamaAlpha / ortalamaBeta : 0;

            // Süre hesapla
            var duration = CalculateDuration(eegData);

            // Metrikleri JSON'a çevir
            var metrics = new
            {
                AnalizTipi = "RahatlamaAnalizi",
                OrtalamaAlpha = Math.Round(ortalamaAlpha, 2),
                OrtalamaBeta = Math.Round(ortalamaBeta, 2),
                AvgLowAlpha = Math.Round(avgLowAlpha, 2),
                AvgHighAlpha = Math.Round(avgHighAlpha, 2),
                AvgLowBeta = Math.Round(avgLowBeta, 2),
                AvgHighBeta = Math.Round(avgHighBeta, 2),
                RahatlamaIndeksi = Math.Round(rahatlamaIndeksi, 2),
                SampleCount = eegData.Count,
                Duration = duration,
                OturumBilgisi = new
                {
                    OturumID = session.OturumID,
                    Kullanici = session.Kullanici?.AdSoyad ?? "Bilinmiyor",
                    DeneyTuru = session.DeneyTuru,
                    ZamanEtiketi = session.ZamanEtiketi
                }
            };

            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);

            // Summary oluştur (AI yoksa basit template)
            string summary;
            if (useAI)
            {
                // TODO: AI servisi entegre edildiğinde burası kullanılacak
                summary = await GenerateAiSummaryAsync(metricsJson, "Rahatlama");
            }
            else
            {
                summary = GenerateSimpleSummary(rahatlamaIndeksi, eegData.Count, duration, "Rahatlama");
            }

            // AnalizSonucu oluştur
            return new AnalizSonucu
            {
                OturumID = sessionId,
                AnalizTipi = "RahatlamaAnalizi",
                Metodoloji = useAI ? "AlphaBetaRatio_v1_AI" : "AlphaBetaRatio_v1",
                MetricsJSON = metricsJson,
                Summary = summary,
                AnalizTarihi = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Dikkat analizi hesaplar (Beta/Alpha ratio)
        /// </summary>
        public async Task<AnalizSonucu> ComputeDikkatAnaliziAsync(int sessionId, bool useAI = false)
        {
            var session = await GetSessionInfoAsync(sessionId);
            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);

            if (eegData.Count < MinimumSamplesForAttention)
            {
                throw new InvalidOperationException($"Dikkat analizi için en az {MinimumSamplesForAttention} sample gereklidir. Mevcut: {eegData.Count}");
            }

            // Ortalama hesapla
            var avgLowAlpha = eegData.Average(e => e.LowAlpha ?? 0);
            var avgHighAlpha = eegData.Average(e => e.HighAlpha ?? 0);
            var avgLowBeta = eegData.Average(e => e.LowBeta ?? 0);
            var avgHighBeta = eegData.Average(e => e.HighBeta ?? 0);

            var ortalamaAlpha = (avgLowAlpha + avgHighAlpha) / 2.0;
            var ortalamaBeta = (avgLowBeta + avgHighBeta) / 2.0;

            // Dikkat skoru hesapla
            var dikkatSkoru = ortalamaAlpha > 0 ? ortalamaBeta / ortalamaAlpha : 0;

            // Süre hesapla
            var duration = CalculateDuration(eegData);

            // Metrikleri JSON'a çevir
            var metrics = new
            {
                AnalizTipi = "DikkatAnalizi",
                OrtalamaBeta = Math.Round(ortalamaBeta, 2),
                OrtalamaAlpha = Math.Round(ortalamaAlpha, 2),
                AvgLowBeta = Math.Round(avgLowBeta, 2),
                AvgHighBeta = Math.Round(avgHighBeta, 2),
                AvgLowAlpha = Math.Round(avgLowAlpha, 2),
                AvgHighAlpha = Math.Round(avgHighAlpha, 2),
                DikkatSkoru = Math.Round(dikkatSkoru, 2),
                SampleCount = eegData.Count,
                Duration = duration,
                OturumBilgisi = new
                {
                    OturumID = session.OturumID,
                    Kullanici = session.Kullanici?.AdSoyad ?? "Bilinmiyor",
                    DeneyTuru = session.DeneyTuru,
                    ZamanEtiketi = session.ZamanEtiketi
                }
            };

            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);

            // Summary oluştur
            string summary;
            if (useAI)
            {
                summary = await GenerateAiSummaryAsync(metricsJson, "Dikkat");
            }
            else
            {
                summary = GenerateSimpleSummary(dikkatSkoru, eegData.Count, duration, "Dikkat");
            }

            return new AnalizSonucu
            {
                OturumID = sessionId,
                AnalizTipi = "DikkatAnalizi",
                Metodoloji = useAI ? "BetaAlphaRatio_v1_AI" : "BetaAlphaRatio_v1",
                MetricsJSON = metricsJson,
                Summary = summary,
                AnalizTarihi = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Engagement indeksi hesaplar (Beta / (Alpha + Theta))
        /// </summary>
        public async Task<AnalizSonucu> ComputeEngagementAnaliziAsync(int sessionId, bool useAI = false)
        {
            var session = await GetSessionInfoAsync(sessionId);
            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);

            if (eegData.Count < MinimumSamplesForEngagement)
            {
                throw new InvalidOperationException($"Engagement analizi için en az {MinimumSamplesForEngagement} sample gereklidir. Mevcut: {eegData.Count}");
            }

            // Ortalama hesapla
            var avgTheta = eegData.Average(e => e.Theta ?? 0);
            var avgLowAlpha = eegData.Average(e => e.LowAlpha ?? 0);
            var avgHighAlpha = eegData.Average(e => e.HighAlpha ?? 0);
            var avgLowBeta = eegData.Average(e => e.LowBeta ?? 0);
            var avgHighBeta = eegData.Average(e => e.HighBeta ?? 0);

            var ortalamaAlpha = (avgLowAlpha + avgHighAlpha) / 2.0;
            var ortalamaBeta = (avgLowBeta + avgHighBeta) / 2.0;
            var ortalamaTheta = avgTheta;

            // Engagement index hesapla
            var denominator = ortalamaAlpha + ortalamaTheta;
            var engagementIndex = denominator > 0 ? ortalamaBeta / denominator : 0;

            // Süre hesapla
            var duration = CalculateDuration(eegData);

            // Metrikleri JSON'a çevir
            var metrics = new
            {
                AnalizTipi = "EngagementAnalizi",
                OrtalamaBeta = Math.Round(ortalamaBeta, 2),
                OrtalamaAlpha = Math.Round(ortalamaAlpha, 2),
                OrtalamaTheta = Math.Round(ortalamaTheta, 2),
                AvgLowBeta = Math.Round(avgLowBeta, 2),
                AvgHighBeta = Math.Round(avgHighBeta, 2),
                AvgLowAlpha = Math.Round(avgLowAlpha, 2),
                AvgHighAlpha = Math.Round(avgHighAlpha, 2),
                EngagementIndex = Math.Round(engagementIndex, 2),
                SampleCount = eegData.Count,
                Duration = duration,
                OturumBilgisi = new
                {
                    OturumID = session.OturumID,
                    Kullanici = session.Kullanici?.AdSoyad ?? "Bilinmiyor",
                    DeneyTuru = session.DeneyTuru,
                    ZamanEtiketi = session.ZamanEtiketi
                }
            };

            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);

            // Summary oluştur
            string summary;
            if (useAI)
            {
                summary = await GenerateAiSummaryAsync(metricsJson, "Engagement");
            }
            else
            {
                summary = GenerateSimpleSummary(engagementIndex, eegData.Count, duration, "Engagement");
            }

            return new AnalizSonucu
            {
                OturumID = sessionId,
                AnalizTipi = "EngagementAnalizi",
                Metodoloji = useAI ? "EngagementIndex_v1_AI" : "EngagementIndex_v1",
                MetricsJSON = metricsJson,
                Summary = summary,
                AnalizTarihi = DateTime.UtcNow
            };
        }

        /// <summary>
        /// <summary>

        /// Stres indeksi hesaplar (High Beta + Gamma / Alpha)

        /// </summary>

        public async Task<AnalizSonucu> ComputeStresAnaliziAsync(int sessionId, bool useAI = false)

        {

            var session = await GetSessionInfoAsync(sessionId);

            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);



            if (eegData.Count < MinimumSamplesForAttention)

            {

                throw new InvalidOperationException($"Stres analizi icin en az {MinimumSamplesForAttention} sample gereklidir. Mevcut: {eegData.Count}");

            }



            var avgLowAlpha = eegData.Average(e => e.LowAlpha ?? 0);

            var avgHighAlpha = eegData.Average(e => e.HighAlpha ?? 0);

            var avgLowBeta = eegData.Average(e => e.LowBeta ?? 0);

            var avgHighBeta = eegData.Average(e => e.HighBeta ?? 0);

            var avgLowGamma = eegData.Average(e => e.LowGamma ?? 0);

            var avgHighGamma = eegData.Average(e => e.HighGamma ?? 0);

            var avgTheta = eegData.Average(e => e.Theta ?? 0);



            var meanAlpha = (avgLowAlpha + avgHighAlpha) / 2.0;

            var meanBeta = (avgLowBeta + avgHighBeta) / 2.0;

            var meanGamma = (avgLowGamma + avgHighGamma) / 2.0;



            var stressIndex = meanAlpha > 0 ? (avgHighBeta + meanGamma) / meanAlpha : 0;

            var duration = CalculateDuration(eegData);



            var metrics = new

            {

                AnalizTipi = "StresAnalizi",

                OrtalamaAlpha = Math.Round(meanAlpha, 2),

                OrtalamaBeta = Math.Round(meanBeta, 2),

                OrtalamaGamma = Math.Round(meanGamma, 2),

                OrtalamaTheta = Math.Round(avgTheta, 2),

                AvgHighBeta = Math.Round(avgHighBeta, 2),

                AvgLowBeta = Math.Round(avgLowBeta, 2),

                AvgHighGamma = Math.Round(avgHighGamma, 2),

                AvgLowGamma = Math.Round(avgLowGamma, 2),

                StresIndeksi = Math.Round(stressIndex, 2),

                SampleCount = eegData.Count,

                Duration = duration,

                OturumBilgisi = new

                {

                    OturumID = session.OturumID,

                    Kullanici = session.Kullanici?.AdSoyad ?? "Bilinmiyor",

                    DeneyTuru = session.DeneyTuru,

                    ZamanEtiketi = session.ZamanEtiketi

                }

            };



            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);



            var summary = useAI

                ? await GenerateAiSummaryAsync(metricsJson, "Stres")

                : GenerateSimpleSummary(stressIndex, eegData.Count, duration, "Stres");



            return new AnalizSonucu

            {

                OturumID = sessionId,

                AnalizTipi = "StresAnalizi",

                Metodoloji = useAI ? "StressIndex_v1_AI" : "StressIndex_v1",

                MetricsJSON = metricsJson,

                Summary = summary,

                AnalizTarihi = DateTime.UtcNow

            };

        }



        /// <summary>

        /// Yorgunluk indeksi hesaplar (Theta + Delta / Beta)

        /// </summary>

        public async Task<AnalizSonucu> ComputeYorgunlukAnaliziAsync(int sessionId, bool useAI = false)

        {

            var session = await GetSessionInfoAsync(sessionId);

            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);



            if (eegData.Count < MinimumSamplesForEngagement)

            {

                throw new InvalidOperationException($"Yorgunluk analizi icin en az {MinimumSamplesForEngagement} sample gereklidir. Mevcut: {eegData.Count}");

            }



            var avgTheta = eegData.Average(e => e.Theta ?? 0);

            var avgDelta = eegData.Average(e => e.Delta ?? 0);

            var avgLowBeta = eegData.Average(e => e.LowBeta ?? 0);

            var avgHighBeta = eegData.Average(e => e.HighBeta ?? 0);

            var avgLowAlpha = eegData.Average(e => e.LowAlpha ?? 0);

            var avgHighAlpha = eegData.Average(e => e.HighAlpha ?? 0);



            var meanBeta = (avgLowBeta + avgHighBeta) / 2.0;

            var meanAlpha = (avgLowAlpha + avgHighAlpha) / 2.0;

            var fatigueIndex = meanBeta > 0 ? (avgTheta + (avgDelta / 2.0)) / meanBeta : 0;

            var duration = CalculateDuration(eegData);



            var metrics = new

            {

                AnalizTipi = "YorgunlukAnalizi",

                OrtalamaBeta = Math.Round(meanBeta, 2),

                OrtalamaAlpha = Math.Round(meanAlpha, 2),

                OrtalamaTheta = Math.Round(avgTheta, 2),

                OrtalamaDelta = Math.Round(avgDelta, 2),

                AvgLowBeta = Math.Round(avgLowBeta, 2),

                AvgHighBeta = Math.Round(avgHighBeta, 2),

                YorgunlukIndeksi = Math.Round(fatigueIndex, 2),

                SampleCount = eegData.Count,

                Duration = duration,

                OturumBilgisi = new

                {

                    OturumID = session.OturumID,

                    Kullanici = session.Kullanici?.AdSoyad ?? "Bilinmiyor",

                    DeneyTuru = session.DeneyTuru,

                    ZamanEtiketi = session.ZamanEtiketi

                }

            };



            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);



            var summary = useAI

                ? await GenerateAiSummaryAsync(metricsJson, "Yorgunluk")

                : GenerateSimpleSummary(fatigueIndex, eegData.Count, duration, "Yorgunluk");



            return new AnalizSonucu

            {

                OturumID = sessionId,

                AnalizTipi = "YorgunlukAnalizi",

                Metodoloji = useAI ? "FatigueIndex_v1_AI" : "FatigueIndex_v1",

                MetricsJSON = metricsJson,

                Summary = summary,

                AnalizTarihi = DateTime.UtcNow

            };

        }



        /// <summary>


        public async Task<AnalizSonucu> ComputeBatchComparisonAsync(
            int userId,
            string experimentType,
            List<int> sessionIds,
            string analysisType,
            int baselineSessionId,
            bool useAI = false,
            bool includeExamResults = true,
            string aiProvider = "openai")
        {
            if (sessionIds == null || sessionIds.Count < 2)
            {
                throw new ArgumentException("Karsilastirma icin en az 2 oturum gereklidir", nameof(sessionIds));
            }

            if (!sessionIds.Contains(baselineSessionId))
            {
                throw new ArgumentException("Bazal oturum, karsilastirma listesinde olmalidir", nameof(baselineSessionId));
            }

            // 1. Bazal Oturumu ve Degeri Al
            var baselineSession = await GetSessionInfoAsync(baselineSessionId);
            var baselineValue = await ComputeIndexForSession(baselineSessionId, analysisType);
            
            // Kullanici adi
            var userName = baselineSession.Kullanici?.AdSoyad ?? "Bilinmiyor";

            // 2. Tum oturumlari don
            var sessionResults = new List<object>();
            var summaryLines = new List<string>();

            foreach (var sessionId in sessionIds)
            {
                var session = await GetSessionInfoAsync(sessionId);
                var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);
                
                // Index hesapla
                var indexValue = await ComputeIndexForSession(sessionId, analysisType);

                // Ozel durumlari handle et
                if (double.IsNaN(indexValue) || double.IsInfinity(indexValue)) indexValue = 0;
                
                var duration = CalculateDuration(eegData);
                var timeLabel = session.ZamanEtiketi ?? $"Oturum {sessionId}";
                var isBaseline = (sessionId == baselineSessionId);

                double changePercent = 0;
                if (!isBaseline && baselineValue > 0)
                {
                    changePercent = ((indexValue - baselineValue) / baselineValue) * 100.0;
                }

                // Ekrana basilacak change text
                var changeText = isBaseline 
                    ? "BAZAL" 
                    : $"{(changePercent >= 0 ? "+" : "")}{changePercent:F1}%";

                // Listeye ekle
                sessionResults.Add(new
                {
                    OturumID = sessionId,
                    ZamanEtiketi = timeLabel,
                    IndexValue = Math.Round(indexValue, 2),
                    IndexName = analysisType,
                    IsBazal = isBaseline,
                    BazalDegisim = isBaseline ? "BAZAL (Referans)" : changeText,
                    ChangePercent = Math.Round(changePercent, 1),
                    SampleCount = eegData.Count,
                    Duration = duration,
                    KayitBaslangic = session.KayitBaslangic
                });

                summaryLines.Add($"{timeLabel}: {indexValue:F2} ({changeText} bazala gore) - {eegData.Count} sample, {duration}");
            }

            if (sessionResults.Count == 0)
            {
                throw new InvalidOperationException("Hic bir oturumda yeterli veri bulunamadi");
            }

            // 3. Metrics JSON
            var batchMetrics = new
            {
                AnalizTipi = $"TopluKarsilastirma_{analysisType}",
                Kullanici = userName,
                DeneyTuru = experimentType ?? "Genel",
                ToplamOturum = sessionResults.Count,
                BazalOturum = new
                {
                    OturumID = baselineSessionId,
                    ZamanEtiketi = baselineSession.ZamanEtiketi ?? "Etiketsiz",
                    BazalDeger = Math.Round(baselineValue, 2)
                },
                OturumSonuclari = sessionResults
            };

            var metricsJson = JsonConvert.SerializeObject(batchMetrics, Formatting.Indented);

            // 4. Summary & AI
            string summary;
            if (useAI && _aiAnalysisService != null)
            {
                try
                {
                    // Provider support if available
                    // if (!string.IsNullOrEmpty(aiProvider)) _aiAnalysisService.SetProvider(aiProvider);

                    string examDataJson = null;
                    if (includeExamResults)
                    {
                        var examData = await _examService.GetExamDataForAnalysisAsync(sessionIds);
                        if (examData != null)
                        {
                            examDataJson = JsonConvert.SerializeObject(examData, Formatting.Indented);
                        }
                    }

                    summary = await _aiAnalysisService.GenerateComparativeSummaryWithExamAsync(
                        userName,
                        experimentType ?? "Genel",
                        metricsJson,
                        examDataJson);
                }
                catch (Exception ex)
                {
                     summary = GenerateBatchSummaryWithoutAI(analysisType, sessionResults, summaryLines, baselineValue, baselineSession.ZamanEtiketi);
                     summary += $"{Environment.NewLine}{Environment.NewLine}[AI hatasi: {ex.Message}]";
                }
            }
            else
            {
                summary = GenerateBatchSummaryWithoutAI(analysisType, sessionResults, summaryLines, baselineValue, baselineSession.ZamanEtiketi);
            }

            // 5. Return
            var methodology = useAI ? $"Batch_{analysisType}_Baseline_AI" : $"Batch_{analysisType}_Baseline";
            if (includeExamResults) methodology += "_WithExam";

            return new AnalizSonucu
            {
                OturumID = null,
                AnalizTipi = $"TopluKarsilastirma_{analysisType}",
                Metodoloji = methodology,
                MetricsJSON = metricsJson,
                Summary = summary,
                AnalizTarihi = DateTime.UtcNow
            };
        }

        private async Task<Oturum> GetSessionInfoAsync(int sessionId)
        {
            using (var context = DbContextFactory.Create())
            {
                var session = await context.Oturum
                    .Include(o => o.Kullanici)
                    .FirstOrDefaultAsync(o => o.OturumID == sessionId);

                if (session == null)
                {
                    throw new InvalidOperationException($"Oturum bulunamadi: {sessionId}");
                }

                return session;
            }
        }

        private string CalculateDuration(List<EEGVerisi> eegData)
        {
            if (eegData == null || eegData.Count == 0)
            {
                return "0dk 0sn";
            }

            var first = eegData.First().KayitZamani;
            var last = eegData.Last().KayitZamani;
            if (last < first)
            {
                last = first;
            }

            var span = last - first;
            return $"{(int)span.TotalMinutes}dk {span.Seconds}sn";
        }

        private async Task<string> GenerateAiSummaryAsync(string metricsJson, string analizTipi)
        {
            if (_aiAnalysisService == null)
            {
                return $"[AI devre disi] {analizTipi} analizi tamamlandi.";
            }

            try
            {
                return await _aiAnalysisService.GenerateSummaryAsync(metricsJson, analizTipi);
            }
            catch (Exception ex)
            {
                return $"[AI hatasi: {ex.Message}] {analizTipi} analizi tamamlandi.";
            }
        }

        /// Basit summary template olusturur (AI olmadan)

        /// </summary>

        private string GenerateSimpleSummary(double indexValue, int sampleCount, string duration, string analizTipi)

        {

            switch (analizTipi)

            {

                case "Rahatlama":

                {

                    var comment = indexValue > 1.5 ? "high - relaxed state" :

                                 indexValue > 1.0 ? "moderate - typical relaxation" :

                                 "low - possible tension";

                    return $"Rahatlama indeksi: {indexValue:F2}. {sampleCount} sample analiz edildi ({duration}). Deger {comment}.";

                }



                case "Dikkat":

                {

                    var comment = indexValue > 1.2 ? "high - focused" :

                                 indexValue > 0.8 ? "moderate - typical attention" :

                                 "low - distracted";

                    return $"Dikkat skoru: {indexValue:F2}. {sampleCount} sample analiz edildi ({duration}). Deger {comment}.";

                }



                case "Engagement":

                {

                    var comment = indexValue > 2.0 ? "high - active participation" :

                                 indexValue > 1.0 ? "moderate - acceptable participation" :

                                 "low - passive state";

                    return $"Engagement indeksi: {indexValue:F2}. {sampleCount} sample analiz edildi ({duration}). Deger {comment}.";

                }



                case "Stres":

                {

                    var comment = indexValue > 1.4 ? "high - elevated stress" :

                                 indexValue > 1.0 ? "moderate - manageable" :

                                 "low - calm";

                    return $"Stres indeksi: {indexValue:F2}. {sampleCount} sample analiz edildi ({duration}). Deger {comment}.";

                }



                case "Yorgunluk":

                {

                    var comment = indexValue > 1.8 ? "high - significant fatigue" :

                                 indexValue > 1.2 ? "moderate - caution" :

                                 "low - alert";

                    return $"Yorgunluk indeksi: {indexValue:F2}. {sampleCount} sample analiz edildi ({duration}). Deger {comment}.";

                }



                default:

                    return $"Analiz tamamlandi. Indeks: {indexValue:F2}, Sample: {sampleCount}, Sure: {duration}";

            }

        }



        /// <summary>        /// <summary>
        /// AI olmadan toplu karşılaştırma summary'si oluşturur (BAZAL referanslı)
        /// </summary>
        private string GenerateBatchSummaryWithoutAI(string analysisType, List<object> sessionResults, List<string> summaryLines, double baselineValue, string baselineLabel)
        {
            if (sessionResults.Count == 0)
            {
                return "Karsilastirilacak veri bulunamadi.";
            }

            // Dinamik olarak değerleri al (bazal hariç)
            var changes = new List<(string Label, double Value, double ChangePercent)>();
            foreach (dynamic result in sessionResults)
            {
                if (!result.IsBazal) // Bazal hariç
                {
                    changes.Add((result.ZamanEtiketi, result.IndexValue, result.ChangePercent));
                }
            }

            // En yüksek pozitif ve negatif değişimleri bul
            var maxChange = changes.OrderByDescending(x => x.ChangePercent).FirstOrDefault();
            var minChange = changes.OrderBy(x => x.ChangePercent).FirstOrDefault();

            var sb = new StringBuilder();
            sb.AppendLine($"=== BAZAL REFERANSLI KARSILASTIRMA: {analysisType} Analizi ===");
            sb.AppendLine();
            sb.AppendLine($"BAZAL OTURUM: {baselineLabel ?? "Etiketsiz"} (Referans Deger: {baselineValue:F2})");
            sb.AppendLine($"Toplam {sessionResults.Count} oturum karsilastirildi ({changes.Count} oturum bazala gore analiz edildi).");
            sb.AppendLine();

            sb.AppendLine("OTURUM SONUCLARI (Bazala Gore):");
            foreach (var line in summaryLines)
            {
                sb.AppendLine($"  • {line}");
            }
            sb.AppendLine();

            sb.AppendLine("BAZAL KARSILASTIRMA:");
            if (maxChange.ChangePercent > 0)
            {
                sb.AppendLine($"  • En buyuk ARTIŞ: {maxChange.Label} (+%{maxChange.ChangePercent:F1} bazala gore)");
            }
            if (minChange.ChangePercent < 0)
            {
                sb.AppendLine($"  • En buyuk AZALMA: {minChange.Label} ({minChange.ChangePercent:F1}% bazala gore)");
            }
            
            // Genel değerlendirme
            var avgChange = changes.Average(x => x.ChangePercent);
            sb.AppendLine($"  • Ortalama degisim: {(avgChange >= 0 ? "+" : "")}{avgChange:F1}% (bazala gore)");
            sb.AppendLine();

            sb.AppendLine("DEĞERLENDİRME:");
            if (avgChange > 10)
            {
                sb.AppendLine($"  ? Bazal olcume gore ORTALAMA %{avgChange:F1} ARTIŞ!");
                sb.AppendLine($"  ? {analysisType} seviyesi bazalin uzerinde.");
                sb.AppendLine($"  ? Genel olarak OLUMLU sonuc.");
            }
            else if (avgChange < -10)
            {
                sb.AppendLine($"  ?? Bazal olcume gore ORTALAMA %{Math.Abs(avgChange):F1} AZALMA!");
                sb.AppendLine($"  ?? {analysisType} seviyesi bazalin altinda.");
                sb.AppendLine($"  ?? Genel olarak OLUMSUZ sonuc.");
            }
            else
            {
                sb.AppendLine($"  ?? Bazal olcume gore anlamli degisim yok (%{Math.Abs(avgChange):F1}).");
                sb.AppendLine($"  ?? {analysisType} seviyesi bazalle benzer.");
                sb.AppendLine($"  ?? Stabil durum gozlendi.");
            }

            return sb.ToString();
        }
        
        #region NeuroIS Analizleri

        private const int MinimumSamplesForNeuroIS = 30;

        /// <summary>
        /// NeuroIS: Bilişsel Yük Analizi (Theta/Alpha ratio)
        /// Yüksek değer = Daha fazla bilişsel yük/zihinsel çaba
        /// </summary>
        public async Task<AnalizSonucu> ComputeThetaAlphaRatioAsync(int sessionId)
        {
            var session = await GetSessionInfoAsync(sessionId);
            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);

            if (eegData.Count < MinimumSamplesForNeuroIS)
            {
                throw new InvalidOperationException($"NeuroIS analizi için en az {MinimumSamplesForNeuroIS} sample gereklidir. Mevcut: {eegData.Count}");
            }

            var avgTheta = eegData.Average(e => e.Theta ?? 0);
            var avgLowAlpha = eegData.Average(e => e.LowAlpha ?? 0);
            var avgHighAlpha = eegData.Average(e => e.HighAlpha ?? 0);
            var meanAlpha = (avgLowAlpha + avgHighAlpha) / 2.0;

            var thetaAlphaRatio = meanAlpha > 0 ? avgTheta / meanAlpha : 0;
            var duration = CalculateDuration(eegData);

            // Standart sapma hesapla
            var ratios = eegData.Select(e => {
                var alpha = ((e.LowAlpha ?? 0) + (e.HighAlpha ?? 0)) / 2.0;
                return alpha > 0 ? (e.Theta ?? 0) / alpha : 0;
            }).ToList();
            var stdDev = CalculateStdDev(ratios);

            var metrics = new
            {
                AnalizTipi = "NeuroIS_BilisselYuk",
                ThetaAlphaRatio = Math.Round(thetaAlphaRatio, 4),
                OrtalamaTheta = Math.Round(avgTheta, 2),
                OrtalamaAlpha = Math.Round(meanAlpha, 2),
                StandartSapma = Math.Round(stdDev, 4),
                VaryansKatsayisi = meanAlpha > 0 ? Math.Round((stdDev / thetaAlphaRatio) * 100, 2) : 0,
                SampleCount = eegData.Count,
                Duration = duration,
                OturumTipi = session.OturumTipi ?? "Belirtilmemis",
                OturumBilgisi = new
                {
                    OturumID = session.OturumID,
                    Kullanici = session.Kullanici?.AdSoyad ?? "Bilinmiyor",
                    DeneyTuru = session.DeneyTuru,
                    ZamanEtiketi = session.ZamanEtiketi,
                    DeneyGrubu = session.Kullanici?.DeneyGrubu?.GrupAdi ?? "Atanmamis"
                }
            };

            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);
            var summary = $"Bilişsel Yük (θ/α): {thetaAlphaRatio:F4} | {eegData.Count} sample | {duration}";

            return new AnalizSonucu
            {
                OturumID = sessionId,
                AnalizTipi = "NeuroIS_BilisselYuk",
                Metodoloji = "ThetaAlphaRatio_NeuroIS_v1",
                MetricsJSON = metricsJson,
                Summary = summary,
                AnalizTarihi = DateTime.UtcNow
            };
        }

        /// <summary>
        /// NeuroIS: Konsantrasyon Analizi (Theta/Beta ratio)
        /// Düşük değer = Daha iyi odaklanma/konsantrasyon
        /// </summary>
        public async Task<AnalizSonucu> ComputeThetaBetaRatioAsync(int sessionId)
        {
            var session = await GetSessionInfoAsync(sessionId);
            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);

            if (eegData.Count < MinimumSamplesForNeuroIS)
            {
                throw new InvalidOperationException($"NeuroIS analizi için en az {MinimumSamplesForNeuroIS} sample gereklidir. Mevcut: {eegData.Count}");
            }

            var avgTheta = eegData.Average(e => e.Theta ?? 0);
            var avgLowBeta = eegData.Average(e => e.LowBeta ?? 0);
            var avgHighBeta = eegData.Average(e => e.HighBeta ?? 0);
            var meanBeta = (avgLowBeta + avgHighBeta) / 2.0;

            var thetaBetaRatio = meanBeta > 0 ? avgTheta / meanBeta : 0;
            var duration = CalculateDuration(eegData);

            var ratios = eegData.Select(e => {
                var beta = ((e.LowBeta ?? 0) + (e.HighBeta ?? 0)) / 2.0;
                return beta > 0 ? (e.Theta ?? 0) / beta : 0;
            }).ToList();
            var stdDev = CalculateStdDev(ratios);

            var metrics = new
            {
                AnalizTipi = "NeuroIS_Konsantrasyon",
                ThetaBetaRatio = Math.Round(thetaBetaRatio, 4),
                OrtalamaTheta = Math.Round(avgTheta, 2),
                OrtalamaBeta = Math.Round(meanBeta, 2),
                StandartSapma = Math.Round(stdDev, 4),
                SampleCount = eegData.Count,
                Duration = duration,
                OturumTipi = session.OturumTipi ?? "Belirtilmemis",
                OturumBilgisi = new
                {
                    OturumID = session.OturumID,
                    Kullanici = session.Kullanici?.AdSoyad ?? "Bilinmiyor",
                    DeneyTuru = session.DeneyTuru,
                    ZamanEtiketi = session.ZamanEtiketi,
                    DeneyGrubu = session.Kullanici?.DeneyGrubu?.GrupAdi ?? "Atanmamis"
                }
            };

            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);
            var summary = $"Konsantrasyon (θ/β): {thetaBetaRatio:F4} | {eegData.Count} sample | {duration}";

            return new AnalizSonucu
            {
                OturumID = sessionId,
                AnalizTipi = "NeuroIS_Konsantrasyon",
                Metodoloji = "ThetaBetaRatio_NeuroIS_v1",
                MetricsJSON = metricsJson,
                Summary = summary,
                AnalizTarihi = DateTime.UtcNow
            };
        }

        /// <summary>
        /// NeuroIS: Uyanıklık/Aktivasyon Analizi (Beta/Alpha ratio)
        /// Yüksek değer = Daha uyanık/aktif zihinsel durum
        /// </summary>
        public async Task<AnalizSonucu> ComputeBetaAlphaRatioAsync(int sessionId)
        {
            var session = await GetSessionInfoAsync(sessionId);
            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);

            if (eegData.Count < MinimumSamplesForNeuroIS)
            {
                throw new InvalidOperationException($"NeuroIS analizi için en az {MinimumSamplesForNeuroIS} sample gereklidir. Mevcut: {eegData.Count}");
            }

            var avgLowAlpha = eegData.Average(e => e.LowAlpha ?? 0);
            var avgHighAlpha = eegData.Average(e => e.HighAlpha ?? 0);
            var avgLowBeta = eegData.Average(e => e.LowBeta ?? 0);
            var avgHighBeta = eegData.Average(e => e.HighBeta ?? 0);
            var meanAlpha = (avgLowAlpha + avgHighAlpha) / 2.0;
            var meanBeta = (avgLowBeta + avgHighBeta) / 2.0;

            var betaAlphaRatio = meanAlpha > 0 ? meanBeta / meanAlpha : 0;
            var duration = CalculateDuration(eegData);

            var ratios = eegData.Select(e => {
                var alpha = ((e.LowAlpha ?? 0) + (e.HighAlpha ?? 0)) / 2.0;
                var beta = ((e.LowBeta ?? 0) + (e.HighBeta ?? 0)) / 2.0;
                return alpha > 0 ? beta / alpha : 0;
            }).ToList();
            var stdDev = CalculateStdDev(ratios);

            var metrics = new
            {
                AnalizTipi = "NeuroIS_Uyaniklik",
                BetaAlphaRatio = Math.Round(betaAlphaRatio, 4),
                OrtalamaBeta = Math.Round(meanBeta, 2),
                OrtalamaAlpha = Math.Round(meanAlpha, 2),
                StandartSapma = Math.Round(stdDev, 4),
                SampleCount = eegData.Count,
                Duration = duration,
                OturumTipi = session.OturumTipi ?? "Belirtilmemis",
                OturumBilgisi = new
                {
                    OturumID = session.OturumID,
                    Kullanici = session.Kullanici?.AdSoyad ?? "Bilinmiyor",
                    DeneyTuru = session.DeneyTuru,
                    ZamanEtiketi = session.ZamanEtiketi,
                    DeneyGrubu = session.Kullanici?.DeneyGrubu?.GrupAdi ?? "Atanmamis"
                }
            };

            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);
            var summary = $"Uyanıklık (β/α): {betaAlphaRatio:F4} | {eegData.Count} sample | {duration}";

            return new AnalizSonucu
            {
                OturumID = sessionId,
                AnalizTipi = "NeuroIS_Uyaniklik",
                Metodoloji = "BetaAlphaRatio_NeuroIS_v1",
                MetricsJSON = metricsJson,
                Summary = summary,
                AnalizTarihi = DateTime.UtcNow
            };
        }

        /// <summary>
        /// NeuroIS: Temporal Faz Analizi (15 dakikalık oturum için 3 aşama)
        /// Erken (0-5dk): Bilgi arama/prompt yazma
        /// Orta (5-10dk): Bilgiyi işleme/anlama
        /// Son (10-15dk): Çözümü uygulama
        /// </summary>
        public async Task<AnalizSonucu> ComputeTemporalPhaseAnalysisAsync(int sessionId)
        {
            var session = await GetSessionInfoAsync(sessionId);
            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 10000);

            if (eegData.Count < MinimumSamplesForNeuroIS)
            {
                throw new InvalidOperationException($"Temporal analiz için en az {MinimumSamplesForNeuroIS} sample gereklidir. Mevcut: {eegData.Count}");
            }

            var orderedData = eegData.OrderBy(e => e.KayitZamani).ToList();
            var startTime = orderedData.First().KayitZamani;

            // 3 aşamaya böl
            var phase1 = orderedData.Where(e => (e.KayitZamani - startTime).TotalMinutes <= 5).ToList();
            var phase2 = orderedData.Where(e => (e.KayitZamani - startTime).TotalMinutes > 5 && (e.KayitZamani - startTime).TotalMinutes <= 10).ToList();
            var phase3 = orderedData.Where(e => (e.KayitZamani - startTime).TotalMinutes > 10).ToList();

            var phases = new[]
            {
                CalculatePhaseMetrics(phase1, "Erken (0-5dk)"),
                CalculatePhaseMetrics(phase2, "Orta (5-10dk)"),
                CalculatePhaseMetrics(phase3, "Son (10-15dk)")
            };

            var duration = CalculateDuration(orderedData);

            var metrics = new
            {
                AnalizTipi = "NeuroIS_TemporalFaz",
                ToplamSample = eegData.Count,
                Duration = duration,
                Fazlar = phases,
                OturumTipi = session.OturumTipi ?? "Belirtilmemis",
                OturumBilgisi = new
                {
                    OturumID = session.OturumID,
                    Kullanici = session.Kullanici?.AdSoyad ?? "Bilinmiyor",
                    DeneyTuru = session.DeneyTuru,
                    ZamanEtiketi = session.ZamanEtiketi,
                    DeneyGrubu = session.Kullanici?.DeneyGrubu?.GrupAdi ?? "Atanmamis"
                }
            };

            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);

            var sb = new StringBuilder();
            sb.AppendLine("Temporal Faz Analizi:");
            foreach (var phase in phases)
            {
                sb.AppendLine($"  {phase.FazAdi}: θ/α={phase.ThetaAlphaRatio:F3}, θ/β={phase.ThetaBetaRatio:F3}, β/α={phase.BetaAlphaRatio:F3} ({phase.SampleCount} sample)");
            }

            return new AnalizSonucu
            {
                OturumID = sessionId,
                AnalizTipi = "NeuroIS_TemporalFaz",
                Metodoloji = "TemporalPhase_NeuroIS_v1",
                MetricsJSON = metricsJson,
                Summary = sb.ToString(),
                AnalizTarihi = DateTime.UtcNow
            };
        }

        /// <summary>
        /// NeuroIS: Bazal Normalize Analiz
        /// Görev oturumunu bazal oturuma göre normalize eder
        /// </summary>
        public async Task<AnalizSonucu> ComputeNormalizedRatiosAsync(int taskSessionId, int basalSessionId)
        {
            var taskSession = await GetSessionInfoAsync(taskSessionId);
            var basalSession = await GetSessionInfoAsync(basalSessionId);

            var taskData = await _eegDataService.GetRecentBySessionAsync(taskSessionId, 5000);
            var basalData = await _eegDataService.GetRecentBySessionAsync(basalSessionId, 5000);

            if (taskData.Count < MinimumSamplesForNeuroIS || basalData.Count < MinimumSamplesForNeuroIS)
            {
                throw new InvalidOperationException($"Normalize analiz için her iki oturumda en az {MinimumSamplesForNeuroIS} sample gereklidir.");
            }

            // Bazal değerler
            var basalThetaAlpha = CalculateThetaAlpha(basalData);
            var basalThetaBeta = CalculateThetaBeta(basalData);
            var basalBetaAlpha = CalculateBetaAlpha(basalData);

            // Görev değerleri
            var taskThetaAlpha = CalculateThetaAlpha(taskData);
            var taskThetaBeta = CalculateThetaBeta(taskData);
            var taskBetaAlpha = CalculateBetaAlpha(taskData);

            // Normalize: (Görev - Bazal) / Bazal
            var normThetaAlpha = basalThetaAlpha > 0 ? (taskThetaAlpha - basalThetaAlpha) / basalThetaAlpha : 0;
            var normThetaBeta = basalThetaBeta > 0 ? (taskThetaBeta - basalThetaBeta) / basalThetaBeta : 0;
            var normBetaAlpha = basalBetaAlpha > 0 ? (taskBetaAlpha - basalBetaAlpha) / basalBetaAlpha : 0;

            var metrics = new
            {
                AnalizTipi = "NeuroIS_NormalizeAnaliz",
                BazalOturum = new
                {
                    OturumID = basalSessionId,
                    ZamanEtiketi = basalSession.ZamanEtiketi,
                    ThetaAlpha = Math.Round(basalThetaAlpha, 4),
                    ThetaBeta = Math.Round(basalThetaBeta, 4),
                    BetaAlpha = Math.Round(basalBetaAlpha, 4),
                    SampleCount = basalData.Count
                },
                GorevOturum = new
                {
                    OturumID = taskSessionId,
                    ZamanEtiketi = taskSession.ZamanEtiketi,
                    ThetaAlpha = Math.Round(taskThetaAlpha, 4),
                    ThetaBeta = Math.Round(taskThetaBeta, 4),
                    BetaAlpha = Math.Round(taskBetaAlpha, 4),
                    SampleCount = taskData.Count
                },
                NormalizeDegerler = new
                {
                    ThetaAlpha = Math.Round(normThetaAlpha, 4),
                    ThetaBeta = Math.Round(normThetaBeta, 4),
                    BetaAlpha = Math.Round(normBetaAlpha, 4),
                    ThetaAlphaYuzde = Math.Round(normThetaAlpha * 100, 1),
                    ThetaBetaYuzde = Math.Round(normThetaBeta * 100, 1),
                    BetaAlphaYuzde = Math.Round(normBetaAlpha * 100, 1)
                },
                Kullanici = taskSession.Kullanici?.AdSoyad ?? "Bilinmiyor",
                DeneyGrubu = taskSession.Kullanici?.DeneyGrubu?.GrupAdi ?? "Atanmamis"
            };

            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);

            var summary = $"Normalize Analiz (Bazal: {basalSession.ZamanEtiketi})\n" +
                         $"  θ/α: {normThetaAlpha * 100:+0.0;-0.0}% | θ/β: {normThetaBeta * 100:+0.0;-0.0}% | β/α: {normBetaAlpha * 100:+0.0;-0.0}%";

            return new AnalizSonucu
            {
                OturumID = taskSessionId,
                AnalizTipi = "NeuroIS_NormalizeAnaliz",
                Metodoloji = "NormalizedRatios_NeuroIS_v1",
                MetricsJSON = metricsJson,
                Summary = summary,
                AnalizTarihi = DateTime.UtcNow
            };
        }

        /// <summary>
        /// NeuroIS: Grup Karşılaştırma Analizi
        /// İki deney grubunun ortalama metriklerini karşılaştırır
        /// </summary>
        public async Task<AnalizSonucu> ComputeNeuroISGroupComparisonAsync(
            List<int> group1SessionIds, 
            List<int> group2SessionIds,
            string group1Name = "Grup 1",
            string group2Name = "Grup 2")
        {
            if (group1SessionIds == null || group1SessionIds.Count == 0)
                throw new ArgumentException("Grup 1 için en az bir oturum gerekli");
            if (group2SessionIds == null || group2SessionIds.Count == 0)
                throw new ArgumentException("Grup 2 için en az bir oturum gerekli");

            var group1Metrics = await CalculateGroupAverageMetrics(group1SessionIds);
            var group2Metrics = await CalculateGroupAverageMetrics(group2SessionIds);

            var metrics = new
            {
                AnalizTipi = "NeuroIS_GrupKarsilastirma",
                Grup1 = new
                {
                    GrupAdi = group1Name,
                    OturumSayisi = group1SessionIds.Count,
                    ToplamSample = group1Metrics.TotalSamples,
                    OrtThetaAlpha = Math.Round(group1Metrics.AvgThetaAlpha, 4),
                    OrtThetaBeta = Math.Round(group1Metrics.AvgThetaBeta, 4),
                    OrtBetaAlpha = Math.Round(group1Metrics.AvgBetaAlpha, 4),
                    StdThetaAlpha = Math.Round(group1Metrics.StdThetaAlpha, 4),
                    StdThetaBeta = Math.Round(group1Metrics.StdThetaBeta, 4),
                    StdBetaAlpha = Math.Round(group1Metrics.StdBetaAlpha, 4)
                },
                Grup2 = new
                {
                    GrupAdi = group2Name,
                    OturumSayisi = group2SessionIds.Count,
                    ToplamSample = group2Metrics.TotalSamples,
                    OrtThetaAlpha = Math.Round(group2Metrics.AvgThetaAlpha, 4),
                    OrtThetaBeta = Math.Round(group2Metrics.AvgThetaBeta, 4),
                    OrtBetaAlpha = Math.Round(group2Metrics.AvgBetaAlpha, 4),
                    StdThetaAlpha = Math.Round(group2Metrics.StdThetaAlpha, 4),
                    StdThetaBeta = Math.Round(group2Metrics.StdThetaBeta, 4),
                    StdBetaAlpha = Math.Round(group2Metrics.StdBetaAlpha, 4)
                },
                Farklar = new
                {
                    ThetaAlphaFark = Math.Round(group1Metrics.AvgThetaAlpha - group2Metrics.AvgThetaAlpha, 4),
                    ThetaBetaFark = Math.Round(group1Metrics.AvgThetaBeta - group2Metrics.AvgThetaBeta, 4),
                    BetaAlphaFark = Math.Round(group1Metrics.AvgBetaAlpha - group2Metrics.AvgBetaAlpha, 4)
                }
            };

            var metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);

            var sb = new StringBuilder();
            sb.AppendLine($"Grup Karşılaştırma: {group1Name} vs {group2Name}");
            sb.AppendLine($"  θ/α: {group1Metrics.AvgThetaAlpha:F4} vs {group2Metrics.AvgThetaAlpha:F4} (Fark: {group1Metrics.AvgThetaAlpha - group2Metrics.AvgThetaAlpha:+0.0000;-0.0000})");
            sb.AppendLine($"  θ/β: {group1Metrics.AvgThetaBeta:F4} vs {group2Metrics.AvgThetaBeta:F4} (Fark: {group1Metrics.AvgThetaBeta - group2Metrics.AvgThetaBeta:+0.0000;-0.0000})");
            sb.AppendLine($"  β/α: {group1Metrics.AvgBetaAlpha:F4} vs {group2Metrics.AvgBetaAlpha:F4} (Fark: {group1Metrics.AvgBetaAlpha - group2Metrics.AvgBetaAlpha:+0.0000;-0.0000})");

            return new AnalizSonucu
            {
                OturumID = null,
                AnalizTipi = "NeuroIS_GrupKarsilastirma",
                Metodoloji = "GroupComparison_NeuroIS_v1",
                MetricsJSON = metricsJson,
                Summary = sb.ToString(),
                AnalizTarihi = DateTime.UtcNow
            };
        }

        #region NeuroIS Helper Methods

        private double CalculateThetaAlpha(List<EEGVerisi> data)
        {
            if (data == null || data.Count == 0) return 0;
            var avgTheta = data.Average(e => e.Theta ?? 0);
            var meanAlpha = data.Average(e => ((e.LowAlpha ?? 0) + (e.HighAlpha ?? 0)) / 2.0);
            return meanAlpha > 0 ? avgTheta / meanAlpha : 0;
        }

        private double CalculateThetaBeta(List<EEGVerisi> data)
        {
            if (data == null || data.Count == 0) return 0;
            var avgTheta = data.Average(e => e.Theta ?? 0);
            var meanBeta = data.Average(e => ((e.LowBeta ?? 0) + (e.HighBeta ?? 0)) / 2.0);
            return meanBeta > 0 ? avgTheta / meanBeta : 0;
        }

        private double CalculateBetaAlpha(List<EEGVerisi> data)
        {
            if (data == null || data.Count == 0) return 0;
            var meanAlpha = data.Average(e => ((e.LowAlpha ?? 0) + (e.HighAlpha ?? 0)) / 2.0);
            var meanBeta = data.Average(e => ((e.LowBeta ?? 0) + (e.HighBeta ?? 0)) / 2.0);
            return meanAlpha > 0 ? meanBeta / meanAlpha : 0;
        }

        private double CalculateStdDev(List<double> values)
        {
            if (values == null || values.Count < 2) return 0;
            var avg = values.Average();
            var sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sumOfSquares / (values.Count - 1));
        }

        private PhaseMetrics CalculatePhaseMetrics(List<EEGVerisi> data, string phaseName)
        {
            if (data == null || data.Count == 0)
            {
                return new PhaseMetrics { FazAdi = phaseName, SampleCount = 0 };
            }

            return new PhaseMetrics
            {
                FazAdi = phaseName,
                SampleCount = data.Count,
                ThetaAlphaRatio = CalculateThetaAlpha(data),
                ThetaBetaRatio = CalculateThetaBeta(data),
                BetaAlphaRatio = CalculateBetaAlpha(data)
            };
        }

        private async Task<GroupMetrics> CalculateGroupAverageMetrics(List<int> sessionIds)
        {
            var allThetaAlpha = new List<double>();
            var allThetaBeta = new List<double>();
            var allBetaAlpha = new List<double>();
            var totalSamples = 0;

            foreach (var sessionId in sessionIds)
            {
                var data = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);
                if (data.Count < 20) continue;

                allThetaAlpha.Add(CalculateThetaAlpha(data));
                allThetaBeta.Add(CalculateThetaBeta(data));
                allBetaAlpha.Add(CalculateBetaAlpha(data));
                totalSamples += data.Count;
            }

            return new GroupMetrics
            {
                TotalSamples = totalSamples,
                AvgThetaAlpha = allThetaAlpha.Count > 0 ? allThetaAlpha.Average() : 0,
                AvgThetaBeta = allThetaBeta.Count > 0 ? allThetaBeta.Average() : 0,
                AvgBetaAlpha = allBetaAlpha.Count > 0 ? allBetaAlpha.Average() : 0,
                StdThetaAlpha = CalculateStdDev(allThetaAlpha),
                StdThetaBeta = CalculateStdDev(allThetaBeta),
                StdBetaAlpha = CalculateStdDev(allBetaAlpha)
            };
        }

        private class PhaseMetrics
        {
            public string FazAdi { get; set; }
            public int SampleCount { get; set; }
            public double ThetaAlphaRatio { get; set; }
            public double ThetaBetaRatio { get; set; }
            public double BetaAlphaRatio { get; set; }
        }

        private class GroupMetrics
        {
            public int TotalSamples { get; set; }
            public double AvgThetaAlpha { get; set; }
            public double AvgThetaBeta { get; set; }
            public double AvgBetaAlpha { get; set; }
            public double StdThetaAlpha { get; set; }
            public double StdThetaBeta { get; set; }
            public double StdBetaAlpha { get; set; }
        }

        #endregion

        #endregion

        /// <summary>
        /// Tek bir oturum için indeks değerini hesaplar (bazal referans için)
        /// </summary>
        private async Task<double> ComputeIndexForSession(int sessionId, string analysisType)
        {
            var eegData = await _eegDataService.GetRecentBySessionAsync(sessionId, 5000);
            
            if (eegData.Count < 20) return 0;

            switch (analysisType)
            {
                case "Rahatlama":
                    var avgLowAlpha = eegData.Average(e => e.LowAlpha ?? 0);
                    var avgHighAlpha = eegData.Average(e => e.HighAlpha ?? 0);
                    var avgLowBeta = eegData.Average(e => e.LowBeta ?? 0);
                    var avgHighBeta = eegData.Average(e => e.HighBeta ?? 0);
                    var ortalamaAlpha = (avgLowAlpha + avgHighAlpha) / 2.0;
                    var ortalamaBeta = (avgLowBeta + avgHighBeta) / 2.0;
                    return ortalamaBeta > 0 ? ortalamaAlpha / ortalamaBeta : 0;

                case "Dikkat":
                    var avgLowAlphaD = eegData.Average(e => e.LowAlpha ?? 0);
                    var avgHighAlphaD = eegData.Average(e => e.HighAlpha ?? 0);
                    var avgLowBetaD = eegData.Average(e => e.LowBeta ?? 0);
                    var avgHighBetaD = eegData.Average(e => e.HighBeta ?? 0);
                    var ortalamaAlphaD = (avgLowAlphaD + avgHighAlphaD) / 2.0;
                    var ortalamaBetaD = (avgLowBetaD + avgHighBetaD) / 2.0;
                    return ortalamaAlphaD > 0 ? ortalamaBetaD / ortalamaAlphaD : 0;

                

                case "Stres":
                    var avgLowAlphaS = eegData.Average(e => e.LowAlpha ?? 0);
                    var avgHighAlphaS = eegData.Average(e => e.HighAlpha ?? 0);
                    var avgHighBetaS = eegData.Average(e => e.HighBeta ?? 0);
                    var avgLowGammaS = eegData.Average(e => e.LowGamma ?? 0);
                    var avgHighGammaS = eegData.Average(e => e.HighGamma ?? 0);
                    var ortalamaAlphaS = (avgLowAlphaS + avgHighAlphaS) / 2.0;
                    var ortalamaGammaS = (avgLowGammaS + avgHighGammaS) / 2.0;
                    return ortalamaAlphaS > 0 ? (avgHighBetaS + ortalamaGammaS) / ortalamaAlphaS : 0;

                case "Yorgunluk":
                    var avgThetaY = eegData.Average(e => e.Theta ?? 0);
                    var avgDeltaY = eegData.Average(e => e.Delta ?? 0);
                    var avgLowBetaY = eegData.Average(e => e.LowBeta ?? 0);
                    var avgHighBetaY = eegData.Average(e => e.HighBeta ?? 0);
                    var ortalamaBetaY = (avgLowBetaY + avgHighBetaY) / 2.0;
                    return ortalamaBetaY > 0 ? (avgThetaY + (avgDeltaY / 2.0)) / ortalamaBetaY : 0;

                default:
                    return 0;
            }
        }
    }
}


