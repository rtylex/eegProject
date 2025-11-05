using System.Collections.Generic;

namespace eegProject.Models
{
    public class ExamData
    {
        public string SinavTuru { get; set; }
        public string Aciklama { get; set; }
        public List<ExamQuestion> Sorular { get; set; }
    }

    public class ExamQuestion
    {
        public int SoruNo { get; set; }
        public string SoruMetni { get; set; }
        public List<string> Siklar { get; set; }
        public string DogruCevap { get; set; } // "A", "B", "C", "D"
    }

    public class ExamAnswer
    {
        public int SoruNo { get; set; }
        public string VerilenCevap { get; set; }
        public string DogruCevap { get; set; }
        public bool Dogru => VerilenCevap == DogruCevap;
    }
}

