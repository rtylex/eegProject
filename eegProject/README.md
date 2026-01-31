# EEG Yönetim Paneli

Mindwave EEG cihazı ile beyin aktivitesi verisi toplama, analiz etme ve sınav modülü içeren kapsamlı bir masaüstü uygulaması.

---

## 📋 İçindekiler

- [Gereksinimler](#gereksinimler)
- [Kurulum](#kurulum)
- [Modüller ve Özellikler](#modüller-ve-özellikler)
- [Kullanım Kılavuzu](#kullanım-kılavuzu)
- [Sorun Giderme](#sorun-giderme)

---

## Gereksinimler

- Visual Studio 2019 veya üzeri
- SQL Server Express (veya SQL Server)
- .NET Framework 4.7.2 veya üzeri
- NeuroSky Mindwave EEG Cihazı (veri toplama için)
- OpenAI API Key (AI analizi için - opsiyonel)

---

## Kurulum

### Veritabanı Kurulumu

1. **SQL Server Management Studio (SSMS)** açın
2. **Databases** → Sağ tık → **Restore Database...**
3. **Device** seçin → `.bak` dosyasını ekleyin
4. **Database** alanına `eegDB` yazın → **OK**

### Connection String Güncelleme

`App.config` dosyasını açın ve `data source` değerini güncelleyin:

```xml
data source=KENDİ-BİLGİSAYAR-ADINIZ\SQLEXPRESS
```

> **Not:** SQL Server adınızı SSMS bağlantı ekranında görebilirsiniz.

### AI Özelliği (Opsiyonel)

`App.config` dosyasına OpenAI API key'inizi ekleyin:
```xml
<add key="OpenAI_ApiKey" value="sk-xxxxx" />
```

---

## Modüller ve Özellikler

### 👥 Kullanıcı Yönetimi
- Kullanıcı ekleme, düzenleme, silme
- Şifre sıfırlama
- Rol tabanlı yetkilendirme (Admin, Yönetici, Kullanıcı)
- Kullanıcı notları

### 📊 Oturum Yönetimi
- Oturum oluşturma ve düzenleme
- Deney türü atama
- Zaman etiketi belirleme (Bazal, Deney, Sonuç vb.)
- Deney grupları yönetimi

### 🧠 EEG Veri Toplama
- Mindwave cihazından gerçek zamanlı veri akışı
- Frekans bantları: Delta, Theta, Alpha, Beta, Gamma
- Göz kırpma algılama (Blink Strength)
- Otomatik veritabanına kayıt

### 📈 Analiz Modülü

#### Tekli Oturum Analizleri:
| Analiz Tipi | Açıklama |
|-------------|----------|
| **Rahatlama** | Alpha/Beta oranı ile rahatlama seviyesi |
| **Dikkat** | Theta/Beta oranı ile dikkat analizi |
| **Engagement** | Beta/(Alpha+Theta) ile bağlılık indeksi |
| **Stres** | High Beta ve Gamma bazlı stres değerlendirmesi |
| **Yorgunluk** | Theta/Alpha oranı ile yorgunluk tespiti |

#### Toplu Karşılaştırma (Bazal Referanslı):
- Birden fazla oturumu bazal oturum ile karşılaştırma
- AI destekli yorumlama (GPT-3.5/GPT-4)
- Sınav sonuçlarını analize dahil etme seçeneği

### 📝 Sınav Modülü

#### Yönetici İşlemleri:
- Oturuma sınav atama
- **Gruba toplu sınav atama** (tüm grup üyelerine tek seferde)
- Tüm atamaları görüntüleme
- Sınav sonuç raporu

#### Kullanıcı İşlemleri:
- Atanan sınavları görme
- EEG kaydı sırasında sınav çözme
- Otomatik cevap kaydetme

### 📤 Dışa Aktarma
- **Excel (.xlsx)**: Tek kullanıcı veya tüm kullanıcılar
- **JSON**: Makine öğrenimi için uygun format
- Zaman etiketi ve deney türü filtreleme
- Çoklu sayfa seçenekleri

### 📜 Denetim Günlükleri
- Tüm işlemlerin kaydı
- Kullanıcı bazlı filtreleme
- Log temizleme

---

## Kullanım Kılavuzu

### 1. Giriş Yapma
Uygulama açıldığında kullanıcı adı ve şifrenizle giriş yapın.

### 2. Kullanıcı Ekleme
1. **Kullanıcılar** sekmesine gidin
2. **Yeni Kullanıcı Ekle** butonuna tıklayın
3. Ad-soyad, e-posta ve rol bilgilerini girin
4. **Kaydet**

### 3. Oturum Oluşturma
1. **Oturumlar** sekmesine gidin
2. **Yeni Oturum** butonuna tıklayın
3. Kullanıcı, deney türü ve zaman etiketi seçin
4. **Kaydet**

### 4. EEG Kaydı Başlatma
1. **EEG Verisi** sekmesine gidin
2. Oturum seçin
3. **Kayıt Başlat** butonuna tıklayın
4. Mindwave cihazının bağlı olduğundan emin olun
5. Kayıt tamamlandığında **Durdur**

### 5. Analiz Yapma
1. **Analizler** sekmesine gidin
2. **Yeni Analiz** butonuna tıklayın
3. Oturum ve analiz tipini seçin
4. (Opsiyonel) AI Yorumu seçin
5. **Analiz Et**

### 6. Toplu Karşılaştırma
1. **Analizler** sekmesinde **Toplu Karşılaştırma** tıklayın
2. Kullanıcı ve deney türü seçin
3. Karşılaştırılacak oturumları işaretleyin
4. **Bazal oturum** seçin (referans noktası)
5. **Karşılaştır**

### 7. Grup Karşılaştırması
1. **Analizler** → **Grup Karşılaştırma**
2. İki deney grubu seçin
3. Karşılaştırma yöntemi seçin (Ham veya Normalize)
4. (Opsiyonel) **Sınav Sonuçlarını Dahil Et** işaretleyin
5. **Karşılaştır**

### 8. Gruba Toplu Sınav Atama
1. **Sınav** sekmesi → **Gruba Toplu Sınav Ata**
2. Deney grubu seçin
3. Sınav JSON dosyası yükleyin
4. Sınav adı ve açıklama girin
5. **Toplu Ata**

### 9. Dışa Aktarma
1. **Dışa Aktar** sekmesine gidin
2. Kullanıcı veya "Tüm Kullanıcılar" seçin
3. Deney türü ve zaman etiketlerini filtreleyin
4. **Excel'e Aktar** veya **JSON'a Aktar**

---

## Sorun Giderme

### "Cannot open database" hatası
- Veritabanı adının `eegDB` olduğundan emin olun
- SQL Server servisinin çalıştığını kontrol edin

### "A network-related error" hatası
- `data source` değerinin doğru olduğunu kontrol edin
- SQL Server Express'in yüklü ve çalışır durumda olduğunu doğrulayın

### "Login failed" hatası
- Windows Authentication'ın aktif olduğundan emin olun
- SSMS üzerinden bağlantı test edin

### Mindwave bağlantı sorunu
- Cihazın Bluetooth ile eşleştiğinden emin olun
- COM port ayarlarını kontrol edin

### AI Analizi çalışmıyor
- `App.config`'de `OpenAI_ApiKey` değerinin doğru olduğunu kontrol edin
- İnternet bağlantınızı kontrol edin

---

## Teknolojiler

- **Dil:** C# (.NET Framework 4.7.2)
- **Veritabanı:** SQL Server + Entity Framework
- **UI:** Windows Forms
- **Excel:** ClosedXML
- **AI:** OpenAI GPT-3.5/4 API
- **EEG:** NeuroSky Mindwave SDK

---

.
