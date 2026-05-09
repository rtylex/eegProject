# EEG ile Kişisel Eğitim Asistanı
 
MindWave Mobile 2 EEG cihazından alınan beyin dalgası verileriyle geleneksel ve yapay zekâ destekli eğitim yöntemlerini gerçek zamanlı olarak karşılaştıran, C# WinForms tabanlı analiz ve raporlama uygulaması.
 
## Proje Hakkında
 
Farklı çalışma yöntemleri arasında hangisinin daha verimli olduğunu **nesnel verilerle** ölçmek mümkün mü?
 
Bu proje, öğrencilerin çalışırken beyin aktivitesini EEG cihazıyla ölçerek geleneksel yöntemler (YouTube, PDF) ile AI destekli yöntemleri bilimsel olarak karşılaştırır. Sezgisel değil, veriyle karar ver.
 
## Nasıl Çalışır?
 
```
MindWave Mobile 2 (EEG)
        ↓
  Bluetooth / Serial
        ↓
  C# WinForms App
        ↓
Gerçek Zamanlı Analiz
 (Attention / Meditation)
 (Delta – Gamma Bantları)
        ↓
  Karşılaştırma Raporu
```
 
## Özellikler
 
✅ **Gerçek Zamanlı EEG Okuma** — MindWave Mobile 2 ile Bluetooth bağlantısı  
✅ **Dikkat (Attention) Takibi** — 0-100 arası odaklanma skoru  
✅ **Meditasyon (Meditation) Takibi** — Zihinsel dinginlik ölçümü  
✅ **Frekans Bandı Analizi** — Delta, Theta, Alpha, Beta, Gamma bantları  
✅ **Oturum Yönetimi** — Farklı çalışma yöntemleri için ayrı oturumlar  
✅ **Karşılaştırma** — Yöntemler arası grafik karşılaştırma  
✅ **Otomatik Raporlama** — Oturum sonu detaylı rapor üretimi  
✅ **Veri Kayıt** — Tüm verileri CSV/Excel formatında dışa aktar  
 
## Teknoloji Stack
 
- **Platform:** Windows Desktop
- **Dil:** C# (.NET Framework)
- **UI:** Windows Forms (WinForms)
- **EEG Cihaz:** NeuroSky MindWave Mobile 2
- **Protokol:** ThinkGear Serial / Bluetooth
- **Grafik:** LiveCharts / ZedGraph
- **Veri:** CSV, Excel Export
## Gereksinimler
 
### Donanım
- NeuroSky MindWave Mobile 2 EEG Cihazı
- Bluetooth bağlantısı olan Windows PC
### Yazılım
- Windows 10/11
- .NET Framework 4.7.2+
- Visual Studio 2019+ (geliştirme için)
- NeuroSky ThinkGear SDK
## Kurulum
 
```bash
# Repository klonla
git clone https://github.com/rtylex/eegProject.git
cd eegProject
 
# Visual Studio ile aç
start eegProject.sln
 
# F5 ile çalıştır
```
 
### MindWave Cihaz Bağlantısı
 
1. MindWave Mobile 2'yi Bluetooth ile eşleştir
2. Uygulamayı başlat
3. **Cihaz Seç** → COM Port'u seç
4. **Bağlan** butonuna tıkla
5. EEG verileri akmaya başlar
## Kullanım Akışı
 
**1. Oturum Başlat**
- Çalışma yöntemini seç (YouTube / PDF / AI Araçları)
- Oturum süresini belirle
- EEG cihazı tak, bağlan
**2. Çalış**
- Seçtiğin yöntemle çalışmaya başla
- Uygulama arka planda EEG verilerini kaydeder
- Ekranda gerçek zamanlı grafikler görürsün
**3. Rapor Al**
- Oturum bitince otomatik rapor üretilir
- Dikkat skoru, meditasyon skoru, bant analizleri
- Önceki oturumlarla karşılaştır
## EEG Metrikleri
 
| Metrik | Açıklama | Aralık |
|---|---|---|
| Attention | Odaklanma seviyesi | 0–100 |
| Meditation | Zihinsel dinginlik | 0–100 |
| Delta | Derin uyku, bilinçdışı | 0.5–4 Hz |
| Theta | Yaratıcılık, hayal | 4–8 Hz |
| Alpha | Rahatlama, hazırlık | 8–13 Hz |
| Beta | Aktif düşünme, dikkat | 13–30 Hz |
| Gamma | Yoğun odak, öğrenme | 30–100 Hz |
 
## Araştırma Amacı
 
Bu proje aşağıdaki soruları yanıtlamayı hedefler:
 
- YouTube izleyerek mi, PDF okuyarak mı, AI ile mi daha iyi öğreniliyor?
- Hangi yöntemde Gamma dalgası (öğrenme) daha yüksek?
- Hangi yöntemde dikkat dağınıklığı daha az?
- Kişiye göre optimal çalışma yöntemi nedir?
## Roadmap
 
- [ ] Yapay zekâ destekli yöntem önerisi
- [ ] Mobil uygulama (Android/iOS)
- [ ] Uzun vadeli trend analizi
- [ ] Çoklu kullanıcı desteği
- [ ] Web dashboard
## İletişim
 
- **Email:** emirhanyirik@outlook.com
- **GitHub:** [@rtylex](https://github.com/rtylex)
- **LinkedIn:** [Emirhan Yirik](https://linkedin.com/in/emirhan-yirik-27021b214/)
## Lisans
 
Özel Lisans — Ticari kullanım yasaklıdır.
 
---
 
**Beyin verileriyle öğren, veriye dayalı karar ver.** 🧠
