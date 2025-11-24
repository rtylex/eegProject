using System.Collections.Generic;

namespace eegProject.Models
{
    public class ExamData
    {
        public string SinavTuru { get; set; }
        public string Aciklama { get; set; }
        public int? ToplamSinavSuresiDakika { get; set; }
        public int? HerCokSeçmeliSoruSuresiSaniye { get; set; }
        public int? HerDogruYanlisSoruSuresiSaniye { get; set; }
        public int? HerKlasikSoruSuresiSaniye { get; set; }
        public List<ExamQuestion> Sorular { get; set; }
    }

    public class ExamQuestion
    {
        public int SoruNo { get; set; }
        public string SoruTipi { get; set; } // "CokSeçmeli", "DogruYanlis", "Klasik"
        public string SoruMetni { get; set; }
        public List<string> Siklar { get; set; } // Çoktan seçmeli için
        public string DogruCevap { get; set; } // "A", "B", "C", "D" veya "Dogru", "Yanlis"
        public List<string> AnahtarKelimeler { get; set; } // Klasik sorular için
        public double ToplamPuan { get; set; } // Sorunun puanı
        public int? MaxSure { get; set; } // Maksimum süre (saniye, opsiyonel)
    }

    public class ExamAnswer
    {
        public int SoruNo { get; set; }
        public string VerilenCevap { get; set; }
        public string DogruCevap { get; set; }
        public bool Dogru => VerilenCevap == DogruCevap;
    }
}

