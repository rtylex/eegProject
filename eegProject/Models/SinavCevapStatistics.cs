namespace eegProject.Models
{
    /// <summary>
    /// Sınav cevaplarının istatistiklerini tutar
    /// </summary>
    public class SinavCevapStatistics
    {
        public int ToplamSoru { get; set; }
        public int DogruSayisi { get; set; }
        public int YanlisSayisi { get; set; }
        public int BosSayisi { get; set; }
        
        public double? OrtalamaCevapSuresi { get; set; }
        
        public double? ToplamPuan { get; set; }
        public double? AlinanPuan { get; set; }
        public double? BasariYuzdesi { get; set; }
        
        public int CokSeçmeliSayisi { get; set; }
        public int DogruYanlisSayisi { get; set; }
        public int KlasikSayisi { get; set; }
    }
}

