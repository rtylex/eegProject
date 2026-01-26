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
    /// AI API ile analiz yorumlama servisi
    /// OpenAI (ChatGPT) ve Google Gemini destekler
    /// </summary>
    internal sealed class AiAnalysisService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private string _provider; // "openai" veya "gemini"
        private string _apiKey;
        private string _model;
        private string _apiUrl;

        private const string OPENAI_URL = "https://api.openai.com/v1/chat/completions";
        private const string GEMINI_URL = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";

        /// <summary>
        /// Varsayılan constructor - App.config'deki ayarları kullanır
        /// </summary>
        public AiAnalysisService() : this(null)
        {
        }

        /// <summary>
        /// Provider seçimiyle constructor
        /// </summary>
        /// <param name="provider">"openai" veya "gemini", null ise App.config'den okur</param>
        public AiAnalysisService(string provider)
        {
            _provider = string.IsNullOrWhiteSpace(provider) 
                ? (ConfigurationManager.AppSettings["AI_Provider"] ?? "openai").ToLowerInvariant()
                : provider.ToLowerInvariant();

            ConfigureProvider(_provider);
            _httpClient.Timeout = TimeSpan.FromSeconds(90);
        }

        /// <summary>
        /// Runtime'da provider değiştirir
        /// </summary>
        public void SetProvider(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
                return;

            var newProvider = provider.ToLowerInvariant();
            if (newProvider != _provider)
            {
                _provider = newProvider;
                ConfigureProvider(_provider);
            }
        }

        private void ConfigureProvider(string provider)
        {
            if (provider == "gemini")
            {
                _apiKey = ConfigurationManager.AppSettings["Gemini_ApiKey"];
                _model = ConfigurationManager.AppSettings["Gemini_Model"] ?? "gemini-1.5-flash";
                _apiUrl = string.Format(GEMINI_URL, _model);

                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    throw new InvalidOperationException(
                        "Gemini API key bulunamadi. App.config'e 'Gemini_ApiKey' ekleyin.\n\n" +
                        "Ornek:\n" +
                        "<appSettings>\n" +
                        "  <add key=\"AI_Provider\" value=\"gemini\" />\n" +
                        "  <add key=\"Gemini_ApiKey\" value=\"AIzaSy...\" />\n" +
                        "  <add key=\"Gemini_Model\" value=\"gemini-1.5-flash\" />\n" +
                        "</appSettings>");
                }
            }
            else
            {
                // OpenAI (varsayilan)
                _apiKey = ConfigurationManager.AppSettings["OpenAI_ApiKey"];
                _model = ConfigurationManager.AppSettings["OpenAI_Model"] ?? "gpt-3.5-turbo";
                _apiUrl = OPENAI_URL;

                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    throw new InvalidOperationException(
                        "OpenAI API key bulunamadi. App.config'e 'OpenAI_ApiKey' ekleyin.\n\n" +
                        "Ornek:\n" +
                        "<appSettings>\n" +
                        "  <add key=\"AI_Provider\" value=\"openai\" />\n" +
                        "  <add key=\"OpenAI_ApiKey\" value=\"sk-proj-xxx...\" />\n" +
                        "  <add key=\"OpenAI_Model\" value=\"gpt-3.5-turbo\" />\n" +
                        "</appSettings>");
                }

                // OpenAI icin HTTP client headers
                lock (_httpClient)
                {
                    if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                    {
                        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                    }
                }
            }
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
            return await CallAiAsync(prompt, maxTokens: 500);
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
            return await CallAiAsync(prompt, maxTokens: 1500);
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
            return await CallAiAsync(prompt, maxTokens: 3000);
        }

        /// <summary>
        /// Provider'a göre doğru API'yi çağırır
        /// </summary>
        private async Task<string> CallAiAsync(string prompt, int maxTokens = 500)
        {
            if (_provider == "gemini")
            {
                return await CallGeminiAsync(prompt, maxTokens);
            }
            else
            {
                return await CallOpenAiAsync(prompt, maxTokens);
            }
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
                    "OpenAI API istegi zaman asimina ugradi (90 saniye). Tekrar deneyin.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"AI yorumu olusturulurken beklenmeyen hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Google Gemini API'ye istek gönderir
        /// </summary>
        private async Task<string> CallGeminiAsync(string prompt, int maxTokens = 500)
        {
            try
            {
                var systemInstruction = "Sen bir EEG analiz uzmanisin. Verilen EEG metriklerini yorumla ve anlasilir Turkce aciklamalar yap. Bilimsel ama sade bir dil kullan.";

                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemInstruction } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    },
                    generationConfig = new
                    {
                        maxOutputTokens = maxTokens,
                        temperature = 0.7
                    }
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gemini API key URL parametresi olarak eklenir
                var urlWithKey = $"{_apiUrl}?key={_apiKey}";

                var response = await _httpClient.PostAsync(urlWithKey, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Gemini API hatasi: {response.StatusCode}\n" +
                        $"Detay: {responseBody}");
                }

                var jsonResponse = JObject.Parse(responseBody);
                var aiText = jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                if (string.IsNullOrWhiteSpace(aiText))
                {
                    throw new InvalidOperationException("Gemini'dan bos yanit geldi");
                }

                return aiText.Trim();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    "Gemini API'ye baglanirken hata olustu. Internet baglantisini kontrol edin.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException(
                    "Gemini API istegi zaman asimina ugradi (90 saniye). Tekrar deneyin.", ex);
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

Format (Her basliktan sonra MUTLAKA yeni satira gec):

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

SONUC VE KAZANAN:
[NET CEVAP: Hangi oturumda performans en iyi? (Orn: ""Dikkat acisindan en iyi performans 30dk oturumunda gozlemlenmistir."")]
[Neden bu oturum kazandi? (Kisa gerekce)]

ONERI: (opsiyonel)
[Pratik oneri]

Maksimum {(hasExamData ? "1500" : "800")} kelime. Turkce, bilimsel ama anlasilir dil. BAZAL vurgusunu unutma! Basliklari ayri satirda yaz.
";
        }

        /// <summary>
        /// API key'in dogru formatta olup olmadigini kontrol eder
        /// </summary>
        public static bool ValidateApiKey(string apiKey, string provider = "openai")
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            if (provider == "gemini")
            {
                // Gemini API key: AIzaSy... formatinda
                return apiKey.StartsWith("AIza") && apiKey.Length > 20;
            }
            else
            {
                // OpenAI API key: sk-proj-... veya sk-...
                return apiKey.StartsWith("sk-") && apiKey.Length > 20;
            }
        }

        /// <summary>
        /// Mevcut provider bilgisini döner
        /// </summary>
        public string GetCurrentProvider()
        {
            return _provider;
        }

        /// <summary>
        /// Mevcut provider display name'ini döner
        /// </summary>
        public string GetCurrentProviderDisplayName()
        {
            return _provider == "gemini" ? "Google Gemini" : "OpenAI ChatGPT";
        }

        /// <summary>
        /// Mevcut model bilgisini döner
        /// </summary>
        public string GetCurrentModel()
        {
            return _model;
        }

        /// <summary>
        /// API baglantisini test eder
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testPrompt = "Merhaba, bu bir test mesajidir. Sadece 'Test basarili' yaz.";
                var response = await CallAiAsync(testPrompt, maxTokens: 20);
                return !string.IsNullOrWhiteSpace(response);
            }
            catch
            {
                return false;
            }
        }
    }
}
