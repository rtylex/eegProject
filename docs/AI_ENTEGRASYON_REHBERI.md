# 🤖 AI Entegrasyon Rehberi - ChatGPT API

## ✅ Tamamlanan Özellikler

- ✅ **AiAnalysisService.cs** - ChatGPT API servisi
- ✅ **App.config** - API key yapılandırması
- ✅ **AnalysisComputationService** - AI entegrasyonu
- ✅ **Form1** - AI checkbox'ı ve kontroller
- ✅ **Hata yönetimi** - API key yoksa graceful degradation

---

## 📝 ADIM 1: OpenAI API Key Alma

### 1.1. OpenAI Hesabı Oluştur
1. https://platform.openai.com adresine git
2. **Sign Up** ile hesap oluştur
3. Email ve telefon doğrulaması yap

### 1.2. API Key Oluştur
1. https://platform.openai.com/api-keys adresine git
2. **"+ Create new secret key"** butonuna tıkla
3. İsim ver (örn: "EEG Project")
4. **Create** butonuna tıkla
5. ⚠️ **ÖNEMLİ:** Key'i kopyala (bir daha gösterilmez!)
   - Format: `sk-proj-abc123...` veya `sk-abc123...`

### 1.3. Kredi Kartı Ekle (Gerekli)
1. https://platform.openai.com/account/billing/overview
2. **"Add payment method"** → Kredi kartı bilgilerini gir
3. Minimum $5 yükle (opsiyonel, kullandıkça ödersiniz)

**Maliyet:** ~0.03 TL/analiz (GPT-3.5-Turbo)

---

## 🔧 ADIM 2: API Key'i Projeye Ekle

### 2.1. App.config Düzenle
1. Visual Studio'da `eegProject/App.config` dosyasını aç
2. `<appSettings>` bölümünü bul:

```xml
<appSettings>
  <!-- OpenAI API Key'inizi buraya girin -->
  <add key="OpenAI_ApiKey" value="" />
  
  <!-- Model: gpt-3.5-turbo (ucuz) veya gpt-4 (daha kaliteli ama pahali) -->
  <add key="OpenAI_Model" value="gpt-3.5-turbo" />
</appSettings>
```

3. API Key'inizi `value=""` içine yapıştır:

```xml
<add key="OpenAI_ApiKey" value="sk-proj-abc123def456..." />
```

4. **Kaydet** (Ctrl+S)

### 2.2. Dosyayı Kopyala (Önemli!)
Build klasörüne kopyalanması için:
1. Solution Explorer'da `App.config`'e sağ tıkla
2. **Properties** → **Copy to Output Directory** → **Copy if newer**

---

## 🚀 ADIM 3: Kullanım

### 3.1. Projeyi Çalıştır
```
F5 veya Debug > Start Debugging
```

### 3.2. Analiz Sekmesine Git
1. **"Analizler"** sekmesini seç
2. **"Analiz Tetikle"** butonuna tıkla

### 3.3. Dialog'da AI Seçenekleri

#### ✅ API Key Varsa:
```
┌─────────────────────────────────────────┐
│  Yeni Analiz                            │
├─────────────────────────────────────────┤
│  Oturum: [Dropdown]                     │
│  Analiz Tipi: [Rahatlama Analizi]      │
│                                         │
│  ☑ AI Yorumu Ekle (~0.03 TL)           │ ← Aktif!
│                                         │
│  Not: AI yorumu ChatGPT ile            │
│  olusturulacaktir.                      │
│  Maliyet: ~0.03 TL/analiz (GPT-3.5)    │
│                                         │
│  [Vazgeç]  [Analiz Et]                  │
└─────────────────────────────────────────┘
```

#### ❌ API Key Yoksa:
```
┌─────────────────────────────────────────┐
│  Yeni Analiz                            │
├─────────────────────────────────────────┤
│  Oturum: [Dropdown]                     │
│  Analiz Tipi: [Rahatlama Analizi]      │
│                                         │
│  ☐ AI Yorumu Ekle (API key eksik)      │ ← Devre dışı
│                                         │
│  Not: AI ozelligi icin App.config'e    │
│  OpenAI_ApiKey eklemeniz gerekiyor.    │
│                                         │
│  [Vazgeç]  [Analiz Et]                  │
└─────────────────────────────────────────┘
```

### 3.4. Analiz Sonuçları

#### Normal Analiz (AI Yok):
```
"Rahatlama İndeksi: 1.93. 127 sample analiz edildi (2dk 7sn). 
Değer yüksek - kullanıcı rahat durumda."
```

#### AI ile Analiz:
```
"Bu oturumda rahatlama indeksi 1.93 olarak ölçülmüştür, 
bu değer ortalamanın üzerinde olup katılımcının rahat bir 
durumda olduğunu gösterir. 127 sample üzerinden yapılan 
analizde tutarlı bir alpha dominansı gözlenmiştir. Beta 
dalgaları düşük seviyede kaldığı için zihinsel aktivite 
minimal ve dinlenme durumu optimal görünmektedir."
```

---

## 💰 Maliyet Hesaplama

### GPT-3.5-Turbo (Önerilen)
- **Input:** $0.0015 / 1K token
- **Output:** $0.002 / 1K token

**Tek Analiz:**
- Prompt: ~300 token
- Response: ~200 token
- **Toplam:** ~$0.001 = **0.03 TL**

**Aylık Tahmin (100 analiz):**
- 100 analiz x $0.001 = **$0.10** = **~3 TL**

### GPT-4 (Daha Kaliteli)
- **Input:** $0.03 / 1K token
- **Output:** $0.06 / 1K token

**Tek Analiz:** ~$0.02 = **0.6 TL**

---

## 🔒 Güvenlik ve Öneriler

### ✅ Yapılması Gerekenler:
1. **API Key'i Git'e ekleme!**
   - `.gitignore`'a `App.config` ekle
   - Veya environment variable kullan

2. **Rate Limiting**
   - OpenAI limitleri: 3 RPM (requests per minute) - Free tier
   - 3500 RPM - Paid tier

3. **Hata Yönetimi**
   - ✅ Zaten eklendi! API hatası olursa basit summary döner

### ⚠️ Dikkat Edilecekler:
- API key'i kimseyle paylaşma
- Public repository'e push etme
- Test için küçük dataset kullan

---

## 🐛 Sorun Giderme

### Problem 1: "API key bulunamadi" Hatası
**Çözüm:**
1. `App.config` dosyasını kontrol et
2. `<add key="OpenAI_ApiKey" value="sk-..." />` eklenmiş mi?
3. Projeyi yeniden build et (Ctrl+Shift+B)

### Problem 2: "OpenAI API'ye baglanirken hata"
**Çözüm:**
1. İnternet bağlantısını kontrol et
2. API key'in geçerli olduğundan emin ol
3. https://platform.openai.com/account/api-keys → Key aktif mi?
4. Kredi kartı eklenmiş mi? Bakiye var mı?

### Problem 3: "Rate limit aşımı"
**Çözüm:**
1. Free tier: 3 istek/dakika limiti
2. Paid tier'a geç: https://platform.openai.com/account/billing
3. Veya bekleme süresi ekle

### Problem 4: Checkbox devre dışı
**Çözüm:**
1. `IsAiAvailable = false` demektir
2. API key eksik veya hatalı
3. App.config'i kontrol et

---

## 📊 Örnek Senaryolar

### Senaryo 1: Tek Oturum Analizi
```csharp
// Kullanıcı:
// 1. "Analiz Tetikle" tıkla
// 2. Oturum seç: "Emirhan - Müzik (1sa)"
// 3. Analiz Tipi: "Rahatlama Analizi"
// 4. ✅ "AI Yorumu Ekle" işaretle
// 5. "Analiz Et" tıkla

// Sonuç (AI ile):
"Bu oturumda rahatlama indeksi 1.93 olarak ölçülmüştür. 
Müzik dinleme aktivitesi sırasında alpha dalgalarında 
belirgin artış gözlenmiştir..."

// Maliyet: ~0.03 TL
```

### Senaryo 2: Çoklu Analiz
```csharp
// 10 farklı oturum analiz et (AI ile)
// Toplam maliyet: 10 x 0.03 TL = 0.30 TL
```

---

## 🎯 Gelecek Özellikler (TODO)

### Sprint 3: Toplu AI Analizi
- [ ] Çoklu oturum karşılaştırma
- [ ] Zaman içinde trend analizi
- [ ] Batch processing (tek seferde birden fazla)
- [ ] Maliyet: ~0.06 TL/batch

**Örnek:**
```
Kullanıcı: Emirhan
Deney: Meditasyon
Oturumlar: Bazal, 30dk, 1sa, 2sa

AI Çıktısı:
"ÖZET: Meditasyon deneyi boyunca %75 artış gözlendi...
BULGULAR: 
• İlk 30 dk: %28 artış
• 30dk-1sa: %20 artış
• 1sa-2sa: %14 artış
ÖNERI: 45-90 dk optimal..."
```

---

## 📚 Referanslar

- **OpenAI API Docs:** https://platform.openai.com/docs
- **Fiyatlandırma:** https://openai.com/pricing
- **Rate Limits:** https://platform.openai.com/docs/guides/rate-limits
- **Best Practices:** https://platform.openai.com/docs/guides/production-best-practices

---

## ✅ Kontrol Listesi

### Kurulum:
- [ ] OpenAI hesabı oluşturuldu
- [ ] API key alındı
- [ ] Kredi kartı eklendi
- [ ] App.config'e key eklendi
- [ ] Build başarılı

### Test:
- [ ] Uygulama çalışıyor
- [ ] Analiz sekmesi açılıyor
- [ ] AI checkbox'ı aktif (yeşil)
- [ ] Normal analiz yapılabiliyor
- [ ] AI analiz yapılabiliyor
- [ ] Sonuç görüntüleniyor

### Production:
- [ ] .gitignore güncel
- [ ] API key güvende
- [ ] Rate limit farkında
- [ ] Hata yönetimi test edildi
- [ ] Maliyet takibi yapılıyor

---

**AI Entegrasyonu Tamamlandı! 🎉**

Sorularınız için: emirhan@example.com (projenize göre düzenleyin)

