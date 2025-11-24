using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eegProject.Services
{
    /// <summary>
    /// ChatGPT API ile analiz yorumlama servisi
    /// </summary>
    internal sealed class AiAnalysisService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";

        public AiAnalysisService()
        {
            // App.config'den API key ve model oku
            _apiKey = ConfigurationManager.AppSettings["OpenAI_ApiKey"];
            _model = ConfigurationManager.AppSettings["OpenAI_Model"] ?? "gpt-3.5-turbo";

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI API key bulunamadi. App.config'e 'OpenAI_ApiKey' ekleyin.\n\n" +
                    "Ornek:\n" +
                    "<appSettings>\n" +
                    "  <add key=\"OpenAI_ApiKey\" value=\"sk-proj-xxx...\" />\n" +
                    "  <add key=\"OpenAI_Model\" value=\"gpt-3.5-turbo\" />\n" +
                    "</appSettings>");
            }

            // HTTP client headers
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }

            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        /// <summary>
        /// Tek oturum için AI yorumu oluşturur
        /// </summary>
        public async Task<string> GenerateSummaryAsync(string metricsJson, string analysisType)
        {
            if (string.IsNullOrWhiteSpace(metricsJson))
            {
                throw new ArgumentException("MetricsJSON bos olamaz", nameof(metricsJson));
            }

            var prompt = BuildSingleSessionPrompt(metricsJson, analysisType);
            return await CallOpenAiAsync(prompt, maxTokens: 500);
        }

        /// <summary>
        /// Çoklu oturum için karşılaştırmalı AI yorumu oluşturur
        /// </summary>
        public async Task<string> GenerateComparativeSummaryAsync(string userName, string experimentType, string analysesJson)
        {
            if (string.IsNullOrWhiteSpace(analysesJson))
            {
                throw new ArgumentException("AnalysesJSON bos olamaz", nameof(analysesJson));
            }

            var prompt = BuildComparativePrompt(userName, experimentType, analysesJson, null);
            return await CallOpenAiAsync(prompt, maxTokens: 800);
        }

        /// <summary>
        /// Çoklu oturum için SINAV VERİSİ DAHİL karşılaştırmalı AI yorumu oluşturur
        /// </summary>
        public async Task<string> GenerateComparativeSummaryWithExamAsync(
            string userName,
            string experimentType,
            string analysesJson,
            string examDataJson)
        {
            if (string.IsNullOrWhiteSpace(analysesJson))
            {
                throw new ArgumentException("AnalysesJSON bos olamaz", nameof(analysesJson));
            }

            var prompt = BuildComparativePrompt(userName, experimentType, analysesJson, examDataJson);
            return await CallOpenAiAsync(prompt, maxTokens: 1200);
        }

        /// <summary>
        /// OpenAI API'ye istek gönderir
        /// </summary>
        private async Task<string> CallOpenAiAsync(string prompt, int maxTokens = 500)
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "Sen bir EEG analiz uzmanisin. Verilen EEG metriklerini yorumla ve anlasilir Turkce aciklamalar yap. Bilimsel ama sade bir dil kullan."
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    max_tokens = maxTokens,
                    temperature = 0.7
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"OpenAI API hatasi: {response.StatusCode}\n" +
                        $"Detay: {responseBody}");
                }

                var jsonResponse = JObject.Parse(responseBody);
                var aiText = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrWhiteSpace(aiText))
                {
                    throw new InvalidOperationException("OpenAI'dan bos yanit geldi");
                }

                return aiText.Trim();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    "OpenAI API'ye baglanirken hata olustu. Internet baglantisini kontrol edin.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException(
                    "OpenAI API istegi zaman asimina ugradi (60 saniye). Tekrar deneyin.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"AI yorumu olusturulurken beklenmeyen hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tek oturum için prompt oluşturur
        /// </summary>
        private string BuildSingleSessionPrompt(string metricsJson, string analysisType)
        {
            string typeDisplay;
            if (analysisType == "Rahatlama")
                typeDisplay = "Rahatlama Analizi (Alpha/Beta Ratio)";
            else if (analysisType == "Dikkat")
                typeDisplay = "Dikkat Analizi (Beta/Alpha Ratio)";
            else if (analysisType == "Engagement")
                typeDisplay = "Engagement Analizi (Beta/(Alpha+Theta))";
            else
                typeDisplay = analysisType;

            return $@"EEG Analiz Sonuclarini Yorumla:

Analiz Tipi: {typeDisplay}

Metrikler:
{metricsJson}

Gorev:
1. Bu analiz sonuclarini 2-3 cumlelik bir paragrafta yorumla
2. Indeks/skor degerinin ne anlama geldigini acikla
3. Kullanicinin durumunu degerlendirmek icin net ifadeler kullan
4. Maksimum 200 kelime
5. Format: Duz metin, Turkce, bilimsel ama anlasilir

Ornek Format:
""Bu oturumda [analiz tipi] indeksi X olarak olculmustur. Bu deger [yuksek/orta/dusuk] seviyede olup, katilimcinin [durum aciklamasi] oldugunu gosterir. [Ek yorum ve oneri].""
";
        }

        /// <summary>
        /// Çoklu oturum için karşılaştırmalı prompt oluşturur (BAZAL REFERANSLI)
        /// </summary>
        private string BuildComparativePrompt(string userName, string experimentType, string analysesJson, string examDataJson = null)
        {
            bool hasExamData = !string.IsNullOrWhiteSpace(examDataJson);

            string examSection = hasExamData ? $@"

!!! SINAV SONUCLARI DAHIL !!!
Sinav Verileri:
{examDataJson}

Onemli: Sinav verilerini EEG metrikleriyle birlikte degerlendir:
- Sinav basarisi ile dikkat/konsantrasyon arasindaki iliskiyi yorumla
- Hangi oturumda hem EEG hem sinav performansi daha iyi?
- Sinav basarisindaki degisim ile EEG metriklerindeki degisim paralel mi?
- Sure yonetimi analizi: time_exceeded fieldina bak, hangi oturumda sure asimi fazla?
- Soru tipi bazinda performans: question_type fieldina gore hangi soru tipinde zayif/guclu?
- Klasik sorularda matching_percentage ve matched_keywords kullanarak anahtar kelime analizini yap
- Ortalama cevap suresi (average_answer_time_seconds) ile EEG metrikleri arasindaki iliskiyi yorumla
" : "";

            return $@"EEG Deney Analizi - BAZAL REFERANSLI Karsilastirmali Rapor

Katilimci: {userName ?? "Bilinmiyor"}
Deney Turu: {experimentType ?? "Genel"}

!!! ONEMLI: Bu analiz BAZAL (baseline) referanslidir!
- BazalOturum fieldindaki deger REFERANS degeridir (kontrol/baslangic durumu)
- Diger tum oturumlar BAZALA GORE yuzde degisim olarak karsilastirilmistir
- ChangePercent fieldi: Pozitif = Bazala gore ARTIŞ, Negatif = Bazala gore AZALMA
{examSection}
Oturum Verileri:
{analysesJson}

Gorev:
1. BAZAL degeri referans al
2. Her oturumdaki BAZALA GORE degisimi analiz et (ChangePercent kullan)
3. Hangi oturumda bazala gore en iyi sonuc var?
4. Hangi oturumda bazala gore dusus var?
5. Genel trend nedir? (Orn: Bazaldan 1 saate %15 artiş, sonra stabil)
6. Deney etkisini BAZALA GORE degerlendir
{(hasExamData ? "7. SINAV SONUCLARI ile EEG metriklerini KORELE et (cok onemli!)" : "")}

Format:
OZET:
[BAZAL referans degeri + genel karsilastirma - 2 cumle]

BAZALA GORE BULGULAR:
- [En yuksek artiş gosteren oturum + yuzde]
- [En dusuk/azalma gosteren oturum + yuzde]
- [Genel trend: Bazala gore ortalama degisim]
{(hasExamData ? @"
SINAV-EEG KORELASYONU:
- [Sinav basarisi en yuksek oturumdaki EEG durumu]
- [EEG ve sinav performansi arasindaki iliski]
- [Hangi kosulda hem EEG hem sinav daha iyi?]
- [Sure yonetimi: Hangi oturumda time_exceeded en az? EEG ile iliskisi nedir?]
- [Soru tipi performansi: Hangi soru tipinde basarili/basarisiz? Neden?]
- [Klasik sorularda anahtar kelime yakalama yetenegindeki degisim]
- [Ortalama cevap suresi trendi: Hizlaniyor mu, yavasliyor mu? EEG'ye etkisi]" : "")}

DEGERLENDIRME:
[Bazala gore genel yorumlama - deney basarili mi? Hangi kosulda daha iyi?{(hasExamData ? " Sinav sonuclari degerlendirmeyi nasil etkiliyor?" : "")}]

ONERI: (opsiyonel)
[Pratik oneri]

Maksimum {(hasExamData ? "800" : "500")} kelime. Turkce, bilimsel ama anlasilir dil. BAZAL vurgusunu unutma!
";
        }

        /// <summary>
        /// API key'in dogru formatta olup olmadigini kontrol eder
        /// </summary>
        public static bool ValidateApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            // OpenAI API key format: sk-proj-... veya sk-...
            return apiKey.StartsWith("sk-") && apiKey.Length > 20;
        }

        /// <summary>
        /// API baglantisini test eder
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testPrompt = "Merhaba, bu bir test mesajidir. Sadece 'Test basarili' yaz.";
                var response = await CallOpenAiAsync(testPrompt, maxTokens: 20);
                return !string.IsNullOrWhiteSpace(response);
            }
            catch
            {
                return false;
            }
        }
    }
}

