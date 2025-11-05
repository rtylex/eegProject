# 🧠 EEG Research Management Platform — PRD v4.11

*(Yönetilebilir, Çok Kullanıcılı, Tek Kanallı EEG Deney Platformu — Modüler, Çok Zaman Etiketli, Kod‑tabanlı Analiz Şablonları, Analiz + Excel/JSON Veri İndirme ve Yönetici Tarafından Kalıcı Silme Özelliği)*

---

## Sürüm Notu
**v4.11** — Analiz şablonları **kod tabanlı** olacak şekilde güncellendi; `AnalizSonucu` esnek `MetricsJSON` yapısını kullanır. Yönetici `hard delete` (kalıcı silme) yetkisine sahiptir; silme akışı Oturum → EEGVerisi → AnalizSonucu kaskadı ile çalışır. `EEGVerisi.KullaniciID` için SQL Server `ON DELETE NO ACTION` kullanılarak "multiple cascade paths" engellenmiştir.

---

## 🎯 1. Amaç

Bu sistem, tek kanallı EEG cihazından (NeuroSky MindWave veya eşdeğer) alınan sinyalleri kaydeden, her kullanıcıyı ve deney oturumlarını yöneten, isteğe bağlı olarak Python tabanlı analizleri çalıştıran, yöneticiye gelişmiş raporlama ve dışa aktarım (Excel/JSON) imkânı sunan eksiksiz bir araştırma platformudur. Sistem eğitim, dikkat, rahatlama, stres, müzik, odaklanma gibi deneysel senaryolara uyarlanabilir.

Hedefler (SMART örnekleri):
- 3 ay içinde platformun temel kayıt, oturum yönetimi ve Excel dışa aktarım fonksiyonlarını tamamlamak.
- Pilot çalışmalar ile kullanıcı başına ort. 2 oturum ve 1000 EEG satırı ile veri toplayıp analiz sonuçlarını doğrulamak.

---

## 👥 2. Roller ve Yetkiler

**Kullanıcı (Katılımcı)**
- Oturum başlatma/durdurma, EEG kaydı alma, canlı sinyal izleme, analiz sonuçlarını görüntüleme.

**Yönetici (Admin)**
- Kullanıcı CRUD (AdSoyad düzenleme dahil), kullanıcı silme (kalıcı), deney tanımları ve zaman etiketleri yönetimi, analiz şablonu seçimi (kod tabanlı şablonlar), analiz tetikleme, Excel/JSON dışa aktarma, analiz modülünü aç/kapat.

**Güvenlik Notu:** Yönetici eylemleri uygulama katmanında yetkilendirme ve audit ile korunmalıdır. `DELETE` işlemleri yalnızca yüksek yetkili admin hesaplarına verilmelidir.

---

## ⚙️ 3. Modül Yapısı (Detaylı)

1. **Kullanıcı Yönetimi**
   - Kayıt, giriş, parola hashleme (BCrypt veya SHA-256 + salt). Yönetici doğrudan `AdSoyad` düzenleyebilir ve kullanıcıyı silebilir.
2. **Deney/Oturum Yönetimi**
   - Deney Türü tanımlama (Eğitim, Rahatlama, Müzik vb.). Zaman etiketleri (Bazal, 30dk, 1saat, 2saat vb.) oluşturma ve oturum ile ilişkilendirme.
3. **EEG Kaydı**
   - NeuroSky MindWave cihazından TCP/IP (127.0.0.1:13854) ile gelen JSON verisinin parse edilip `EEGVerisi` tablosuna satır bazında kaydı.
   - Sinyal kalite metriği (opsiyonel) cihazdan geliyorsa kaydedilir; yoksa kalite hesaplama server-side yapılır.
4. **Veri Görselleştirme**
   - Gerçek zamanlı band grafikleri (Delta, Theta, LowAlpha, HighAlpha, LowBeta, HighBeta, LowGamma, HighGamma).
   - Oturum zaman serisi görselleştirme ve zoom/pan.
5. **Analiz Modülü (Yönetici Kontrollü)**
   - Analiz şablonları **kod tabanlı** JSON dosyaları olarak `/repo/config/analysis_templates/` içinde tutulur.
   - Worker (Celery/RQ veya benzeri) kuyruğu kullanılarak arka planda Python analizleri çalıştırılır.
   - Analiz sonuçları `MetricsJSON` + `Summary` ile `AnalizSonucu` tablosuna kaydedilir.
6. **Raporlama & Dışa Aktarım**
   - Yönetici seçtiği kullanıcı + deney türü + zaman etiketlerine göre filtreleyip Excel (.xlsx) formatında çoklu sheet export yapabilir (her sheet bir zaman etiketi). Ayrıca `.json` export da opsiyoneldir.
7. **Yönetici Paneli**
   - Kullanıcı listesi (filtre/arama/sıralama), oturum görüntüleme, analiz tetikleme, şablon seçimi, export, kullanıcı silme (kalıcı).

---

## 🗄️ 4. Veri Modeli — Tablolar (Tam)

Aşağıdaki tablolar PRD v4.11 kapsamındadır. SQL Server uyumlu veri tipleri verilmiştir.

### Tablo: Kullanici
```sql
CREATE TABLE dbo.Kullanici (
  KullaniciID INT IDENTITY(1,1) PRIMARY KEY,
  AdSoyad NVARCHAR(100) NOT NULL,
  Email NVARCHAR(100) NULL,
  SifreHash NVARCHAR(255) NOT NULL,
  KayitTarihi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  Rol NVARCHAR(20) NOT NULL -- 'Kullanici' / 'Admin'
);
```

### Tablo: Yonetici
```sql
CREATE TABLE dbo.Yonetici (
  YoneticiID INT IDENTITY(1,1) PRIMARY KEY,
  KullaniciAdi NVARCHAR(100) NOT NULL,
  Email NVARCHAR(100) NULL,
  SifreHash NVARCHAR(255) NOT NULL,
  YetkiSeviyesi NVARCHAR(20) NULL,
  KayitTarihi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  AnalizModuluAktifMi BIT NOT NULL DEFAULT 0
);
```

### Tablo: Oturum
```sql
CREATE TABLE dbo.Oturum (
  OturumID INT IDENTITY(1,1) PRIMARY KEY,
  KullaniciID INT NOT NULL,
  ZamanEtiketi NVARCHAR(50) NULL,
  DeneyTuru NVARCHAR(100) NULL,
  KayitBaslangic DATETIME2 NULL,
  KayitBitis DATETIME2 NULL,
  Notlar NVARCHAR(MAX) NULL
);
```

### Tablo: EEGVerisi
```sql
CREATE TABLE dbo.EEGVerisi (
  EEGID INT IDENTITY(1,1) PRIMARY KEY,
  OturumID INT NOT NULL,
  KullaniciID INT NOT NULL,
  Delta FLOAT NULL,
  Theta FLOAT NULL,
  LowAlpha FLOAT NULL,
  HighAlpha FLOAT NULL,
  LowBeta FLOAT NULL,
  HighBeta FLOAT NULL,
  LowGamma FLOAT NULL,
  HighGamma FLOAT NULL,
  BlinkStrength INT NULL,
  KayitZamani DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

### Tablo: AnalizSonucu
```sql
CREATE TABLE dbo.AnalizSonucu (
  AnalizID INT IDENTITY(1,1) PRIMARY KEY,
  OturumID INT NULL,
  AnalizTipi NVARCHAR(100) NULL,
  Metodoloji NVARCHAR(200) NULL,
  MetricsJSON NVARCHAR(MAX) NULL,
  Summary NVARCHAR(MAX) NULL,
  AnalizTarihi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

---

## 🔗 5. İlişki Kuralları (FK) ve Silme Davranışı

- `Oturum.KullaniciID` → `Kullanici.KullaniciID`  **(ON DELETE CASCADE)**
- `EEGVerisi.OturumID` → `Oturum.OturumID`  **(ON DELETE CASCADE)**
- `AnalizSonucu.OturumID` → `Oturum.OturumID`  **(ON DELETE CASCADE)**
- `EEGVerisi.KullaniciID` → `Kullanici.KullaniciID`  **(ON DELETE NO ACTION)**

**Açıklama:** SQL Server’ın "multiple cascade paths" kısıtlamasını önlemek için `EEGVerisi.KullaniciID` üzerinde CASCADE bırakılmadı. Kullanici silindiğinde kaskad Oturum üzerinden EEGVerisi ve AnalizSonucu kayıtlarını temizler.

---

## 📈 6. Sistem Akışı (Detaylı)

1. Yönetici giriş yapar (auth).
2. Yönetici kullanıcıyı seçebilir; `AdSoyad` gibi alanları düzenleyebilir.
3. Yönetici analiz şablonunu (kod tabanlı) seçer ve analiz tetikler (tekli veya çoklu oturum).
4. Kullanıcı oturum başlatır; EEG verileri `EEGVerisi` tablosuna kaydedilir.
5. Yönetici seçilen oturumları işaretleyip `Dışa Aktar` der; sistem `WHERE KullaniciID=@id AND DeneyTuru=@tur AND ZamanEtiketi IN (...)` filtre ile veriyi çeker ve Excel/JSON oluşturur.
6. Yönetici `DELETE` ile kullanıcıyı kalıcı olarak sildiğinde (uygulama aynı SQL bağlantısı ile) Oturumlar CASCADE ile silinir ve bağlı EEG/Analiz kayıtları temizlenir.

---

## 🧮 7. Analiz Modülü (Kod‑tabanlı Şablonlar)

**Konsept:** Analiz şablonları JSON formatında kod deposunda saklanır (`/repo/config/analysis_templates/`). Yönetici UI bu şablonları sadece seçer. Analiz worker şablonu parse edip hesaplamaları gerçekleştirir.

**Örnek şablon (RahatlamaAnalizi):**
```json
{
  "AnalizTipi": "RahatlamaAnalizi",
  "Metodoloji": "AlphaBetaRatio_v1",
  "MinSamples": 20,
  "Compute": [
    {"name":"OrtalamaAlpha","expr":"mean(LowAlpha + HighAlpha)"},
    {"name":"OrtalamaBeta","expr":"mean(LowBeta + HighBeta)"},
    {"name":"RahatlamaIndeksi","expr":"OrtalamaAlpha / OrtalamaBeta"}
  ],
  "Stats": ["normality_shapiro","paired_t_test"]
}
```

**Worker akışı:**
- Job kuyruğa alınır → worker DB’den ilgili `EEGVerisi` satırlarını çeker → hesaplamaları yapar → `MetricsJSON` ve `Summary` ile `AnalizSonucu` kaydeder.

---

## 📦 8. Excel (.xlsx) Dışa Aktarım — Çıktı Formatı

- Yönetici seçtiği zaman etiketleri için her etiket ayrı sheet olacak şekilde `.xlsx` dosyası oluşturur.
- Her sheet sütunları: `Delta | Theta | LowAlpha | HighAlpha | LowBeta | HighBeta | LowGamma | HighGamma | BlinkStrength | SignalQuality (ops) | KayitZamani`.
- Üst meta alanı: `Kullanıcı: [Ad Soyad] | Deney Türü: [Rahatlama] | Oluşturulma: [Tarih Saat]`.
- Opsiyon: `MetricsJSON` veya `Summary` ayrı sheet veya aynı dosyada `.json` ayrı dosya olarak verilebilir.

---

## 🔧 9. Teknik Gereksinimler & BAĞLANTILAR

- Backend: .NET Framework 4.8 (API), Python 3.10+ (analiz worker)
- DB: SQL Server (LocalDB veya sunucu)
- Raporlama: OpenXML SDK / ClosedXML
- Cihaz: NeuroSky MindWave (127.0.0.1:13854)
- Kimlik Doğrulama: BCrypt/SHA-256
- Queue: Redis + Celery / RQ veya Azure Queue

---

## 🛠️ 10. Tam Oluşturma SQL (Appendix — SSMS ile çalıştırılabilir)
Aşağıda PRD’ye uygun, idempotent, SQL Server uyumlu `CREATE TABLE` bloğu bulunur. `USE [YourDatabase];` ile hedef veritabanını ayarlayıp çalıştırın.

```sql
USE [YourDatabase];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- (Kullanici, Yonetici, Oturum, EEGVerisi, AnalizSonucu tabloları oluşturma)
-- Script v4.11: idempotent, FK'ler uygun şekilde eklendi (EEGVerisi.KullaniciID -> NO ACTION)

-- 1) Kullanici
IF OBJECT_ID('dbo.Kullanici','U') IS NULL
BEGIN
    CREATE TABLE dbo.Kullanici (
        KullaniciID INT IDENTITY(1,1) PRIMARY KEY,
        AdSoyad NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NULL,
        SifreHash NVARCHAR(255) NOT NULL,
        KayitTarihi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Rol NVARCHAR(20) NOT NULL
    );
END;

-- 2) Yonetici
IF OBJECT_ID('dbo.Yonetici','U') IS NULL
BEGIN
    CREATE TABLE dbo.Yonetici (
        YoneticiID INT IDENTITY(1,1) PRIMARY KEY,
        KullaniciAdi NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NULL,
        SifreHash NVARCHAR(255) NOT NULL,
        YetkiSeviyesi NVARCHAR(20) NULL,
        KayitTarihi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        AnalizModuluAktifMi BIT NOT NULL DEFAULT 0
    );
END;

-- 3) Oturum
IF OBJECT_ID('dbo.Oturum','U') IS NULL
BEGIN
    CREATE TABLE dbo.Oturum (
        OturumID INT IDENTITY(1,1) PRIMARY KEY,
        KullaniciID INT NOT NULL,
        ZamanEtiketi NVARCHAR(50) NULL,
        DeneyTuru NVARCHAR(100) NULL,
        KayitBaslangic DATETIME2 NULL,
        KayitBitis DATETIME2 NULL,
        Notlar NVARCHAR(MAX) NULL
    );
END;

IF OBJECT_ID('dbo.Kullanici','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk WHERE fk.name = 'FK_Oturum_Kullanici')
    BEGIN
        ALTER TABLE dbo.Oturum
        ADD CONSTRAINT FK_Oturum_Kullanici FOREIGN KEY (KullaniciID)
            REFERENCES dbo.Kullanici(KullaniciID) ON DELETE CASCADE;
    END
END;

-- 4) EEGVerisi
IF OBJECT_ID('dbo.EEGVerisi','U') IS NULL
BEGIN
    CREATE TABLE dbo.EEGVerisi (
        EEGID INT IDENTITY(1,1) PRIMARY KEY,
        OturumID INT NOT NULL,
        KullaniciID INT NOT NULL,
        Delta FLOAT NULL,
        Theta FLOAT NULL,
        LowAlpha FLOAT NULL,
        HighAlpha FLOAT NULL,
        LowBeta FLOAT NULL,
        HighBeta FLOAT NULL,
        LowGamma FLOAT NULL,
        HighGamma FLOAT NULL,
        BlinkStrength INT NULL,
        KayitZamani DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.Oturum','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk WHERE fk.name = 'FK_EEG_Oturum')
    BEGIN
        ALTER TABLE dbo.EEGVerisi
        ADD CONSTRAINT FK_EEG_Oturum FOREIGN KEY (OturumID)
            REFERENCES dbo.Oturum(OturumID) ON DELETE CASCADE;
    END
END

IF OBJECT_ID('dbo.Kullanici','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk WHERE fk.name = 'FK_EEG_Kullanici')
    BEGIN
        ALTER TABLE dbo.EEGVerisi
        ADD CONSTRAINT FK_EEG_Kullanici FOREIGN KEY (KullaniciID)
            REFERENCES dbo.Kullanici(KullaniciID) ON DELETE NO ACTION;
    END
END;

-- 5) AnalizSonucu
IF OBJECT_ID('dbo.AnalizSonucu','U') IS NULL
BEGIN
    CREATE TABLE dbo.AnalizSonucu (
        AnalizID INT IDENTITY(1,1) PRIMARY KEY,
        OturumID INT NULL,
        AnalizTipi NVARCHAR(100) NULL,
        Metodoloji NVARCHAR(200) NULL,
        MetricsJSON NVARCHAR(MAX) NULL,
        Summary NVARCHAR(MAX) NULL,
        AnalizTarihi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.Oturum','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk WHERE fk.name = 'FK_Analiz_Oturum')
    BEGIN
        ALTER TABLE dbo.AnalizSonucu
        ADD CONSTRAINT FK_Analiz_Oturum FOREIGN KEY (OturumID)
            REFERENCES dbo.Oturum(OturumID) ON DELETE CASCADE;
    END
END;

COMMIT TRANSACTION;
GO
```

---

## 📚 11. Örnek Analiz Şablonları (Kod‑tabanlı)
Aşağıdaki örnekler `/repo/config/analysis_templates/` içinde `.json` olarak saklanabilir ve yönetici UI tarafından seçilebilir.

### 1) RahatlamaAnalizi
```json
{ "AnalizTipi":"RahatlamaAnalizi", "Metodoloji":"AlphaBetaRatio_v1", "MinSamples":20, "Compute":[{"name":"OrtalamaAlpha","expr":"mean(LowAlpha + HighAlpha)"},{"name":"OrtalamaBeta","expr":"mean(LowBeta + HighBeta)"},{"name":"RahatlamaIndeksi","expr":"OrtalamaAlpha / OrtalamaBeta"}], "Stats":["normality_shapiro","paired_t_test"] }
```

### 2) DikkatAnalizi
```json
{ "AnalizTipi":"DikkatAnalizi", "Metodoloji":"BetaAlphaRatio_v1", "MinSamples":20, "Compute":[{"name":"OrtalamaBeta","expr":"mean(LowBeta + HighBeta)"},{"name":"OrtalamaAlpha","expr":"mean(LowAlpha + HighAlpha)"},{"name":"DikkatSkoru","expr":"OrtalamaBeta / OrtalamaAlpha"}], "Stats":["normality_shapiro","paired_t_test"] }
```

### 3) EngagementIndex
```json
{ "AnalizTipi":"EngagementIndex", "Metodoloji":"Engagement_v1", "MinSamples":30, "Compute":[{"name":"Theta","expr":"mean(Theta)"},{"name":"Alpha","expr":"mean(LowAlpha + HighAlpha)"},{"name":"Beta","expr":"mean(LowBeta + HighBeta)"},{"name":"EngagementIndex","expr":"Beta / (Alpha + Theta)"}], "Stats":["normality_shapiro","t_test_pairwise"] }
```

---

## 🔎 12. Test & Operasyon Notları
- **Test DB**: Script çalıştırılmadan önce test veritabanında denenmelidir.
- **Audit / Log**: Yönetici silme işlemleri için `AdminDeletionLog` gibi bir log tablosu ve `DeleteUserAndLog` stored proc önerilir.
- **Yedekleme**: Hard delete politikasına göre düzenli yedekleme şarttır.
- **Performans**: Büyük veri silmelerinde batch delete veya arka plan job önerilir.

---

## ✅ 13. Sonuç ve Sonraki Adımlar
- PRD v4.11 Canvas’a kaydedildi ve kod‑tabanlı analiz şablonları, Excel/JSON export, analiz worker akışı ve DB şeması güncellendi.
- Sonraki adımlar önerisi:
  1. SQL script’in test DB’de çalıştırılması.
  2. Analiz şablonlarının repo içine konması + JSON schema validasyonu yazılması.
  3. Worker kodu (analiz.py) ile şablon parser entegrasyonu.

---

*PRD v4.11 — Canvas'a kaydedildi.*