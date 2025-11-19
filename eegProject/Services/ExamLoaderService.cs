using System;
using System.IO;
using Newtonsoft.Json;
using eegProject.Models;

namespace eegProject.Services
{
    internal sealed class ExamLoaderService
    {
        public ExamData LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Sinav dosyasi bulunamadi", filePath);

            var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            var examData = JsonConvert.DeserializeObject<ExamData>(json);

            if (examData == null || examData.Sorular == null || examData.Sorular.Count == 0)
                throw new InvalidOperationException("Sinav dosyasi gecersiz veya bos");

            // Soru numaralarını otomatik ata
            for (int i = 0; i < examData.Sorular.Count; i++)
            {
                if (examData.Sorular[i].SoruNo == 0)
                    examData.Sorular[i].SoruNo = i + 1;
            }

            return examData;
        }

        public string GetSampleJsonFormat()
        {
            var sample = new ExamData
            {
                SinavTuru = "Karma Sınav",
                Aciklama = "Çoktan seçmeli, doğru-yanlış ve klasik sorular içeren örnek sınav",
                Sorular = new System.Collections.Generic.List<ExamQuestion>
                {
                    new ExamQuestion
                    {
                        SoruNo = 1,
                        SoruTipi = "CokSeçmeli",
                        SoruMetni = "2 + 2 kaç eder?",
                        Siklar = new System.Collections.Generic.List<string> { "2", "3", "4", "5" },
                        DogruCevap = "C",
                        ToplamPuan = 5,
                        MaxSure = 30
                    },
                    new ExamQuestion
                    {
                        SoruNo = 2,
                        SoruTipi = "DogruYanlis",
                        SoruMetni = "Dünya güneşin etrafında döner.",
                        DogruCevap = "Dogru",
                        ToplamPuan = 3,
                        MaxSure = 20
                    },
                    new ExamQuestion
                    {
                        SoruNo = 3,
                        SoruTipi = "DogruYanlis",
                        SoruMetni = "Su 100 derecede kaynar.",
                        DogruCevap = "Dogru",
                        ToplamPuan = 3,
                        MaxSure = 20
                    },
                    new ExamQuestion
                    {
                        SoruNo = 4,
                        SoruTipi = "Klasik",
                        SoruMetni = "Fotosentez nedir? Kısaca açıklayınız.",
                        AnahtarKelimeler = new System.Collections.Generic.List<string> 
                        { 
                            "ışık", "güneş", "klorofil", "oksijen", 
                            "glikoz", "bitki", "karbon dioksit" 
                        },
                        ToplamPuan = 10,
                        MaxSure = 120
                    },
                    new ExamQuestion
                    {
                        SoruNo = 5,
                        SoruTipi = "CokSeçmeli",
                        SoruMetni = "Türkiye'nin başkenti neresidir?",
                        Siklar = new System.Collections.Generic.List<string> { "İstanbul", "Ankara", "İzmir", "Bursa" },
                        DogruCevap = "B",
                        ToplamPuan = 5,
                        MaxSure = 30
                    }
                }
            };

            return JsonConvert.SerializeObject(sample, Formatting.Indented);
        }
    }
}



