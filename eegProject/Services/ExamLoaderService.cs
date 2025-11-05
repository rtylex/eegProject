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
                SinavTuru = "Matematik",
                Aciklama = "Temel matematik sorulari",
                Sorular = new System.Collections.Generic.List<ExamQuestion>
                {
                    new ExamQuestion
                    {
                        SoruNo = 1,
                        SoruMetni = "2 + 2 kaç eder?",
                        Siklar = new System.Collections.Generic.List<string> { "2", "3", "4", "5" },
                        DogruCevap = "C"
                    },
                    new ExamQuestion
                    {
                        SoruNo = 2,
                        SoruMetni = "5 x 3 kaç eder?",
                        Siklar = new System.Collections.Generic.List<string> { "8", "15", "20", "25" },
                        DogruCevap = "B"
                    },
                    new ExamQuestion
                    {
                        SoruNo = 3,
                        SoruMetni = "10 - 7 kaç eder?",
                        Siklar = new System.Collections.Generic.List<string> { "1", "2", "3", "4" },
                        DogruCevap = "C"
                    }
                }
            };

            return JsonConvert.SerializeObject(sample, Formatting.Indented);
        }
    }
}



