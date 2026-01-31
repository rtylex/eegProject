using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    /// <summary>
    /// Yönetici sınav atama servisi
    /// </summary>
    internal sealed class SinavAtamaService
    {
        /// <summary>
        /// Kullanıcıya sınav atar (Genel atama - eski sistem)
        /// </summary>
        public async Task<SinavAtama> CreateAsync(
            int kullaniciId,
            string sinavAdi,
            string sinavAciklama,
            string sinavJsonPath,
            string sinavJsonContent,
            int atayanYoneticiId,
            DateTime? sonGecerlilikTarihi = null,
            string notlar = null)
        {
            using (var context = DbContextFactory.Create())
            {
                var atama = new SinavAtama
                {
                    KullaniciID = kullaniciId,
                    OturumID = null, // Genel atama
                    SinavAdi = sinavAdi,
                    SinavAciklama = sinavAciklama,
                    SinavJsonPath = sinavJsonPath,
                    SinavJsonContent = sinavJsonContent,
                    AtayanYoneticiID = atayanYoneticiId,
                    AtamaTarihi = DateTime.UtcNow,
                    SonGecerlilikTarihi = sonGecerlilikTarihi,
                    TamamlandiMi = false,
                    Notlar = notlar
                };

                context.SinavAtama.Add(atama);
                await context.SaveChangesAsync();
                return atama;
            }
        }

        /// <summary>
        /// Oturuma sınav atar (YENİ SISTEM - Önerilen)
        /// </summary>
        public async Task<SinavAtama> CreateForSessionAsync(
            int oturumId,
            string sinavAdi,
            string sinavAciklama,
            string sinavJsonPath,
            string sinavJsonContent,
            int atayanYoneticiId,
            string notlar = null)
        {
            using (var context = DbContextFactory.Create())
            {
                // Önce oturumu bulup kullanıcı ID'sini alalım
                var oturum = await context.Oturum.FindAsync(oturumId);
                if (oturum == null)
                {
                    throw new InvalidOperationException($"Oturum bulunamadı: {oturumId}");
                }

                var atama = new SinavAtama
                {
                    KullaniciID = oturum.KullaniciID, // Oturumun kullanıcısı
                    OturumID = oturumId,
                    SinavAdi = sinavAdi,
                    SinavAciklama = sinavAciklama,
                    SinavJsonPath = sinavJsonPath,
                    SinavJsonContent = sinavJsonContent,
                    AtayanYoneticiID = atayanYoneticiId,
                    AtamaTarihi = DateTime.UtcNow,
                    SonGecerlilikTarihi = null, // Oturum bazlı atamalarda geçerlilik yok
                    TamamlandiMi = false,
                    Notlar = notlar
                };

                context.SinavAtama.Add(atama);
                await context.SaveChangesAsync();
                return atama;
            }
        }

        /// <summary>
        /// Kullanıcının atanmış sınavlarını getirir
        /// </summary>
        public async Task<List<SinavAtama>> GetByUserAsync(int kullaniciId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavAtama
                    .Include(a => a.Kullanici)
                    .Include(a => a.Kullanici1) // AtayanYonetici
                    .Include(a => a.Oturum)
                    .Where(a => a.KullaniciID == kullaniciId)
                    .OrderByDescending(a => a.AtamaTarihi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Tamamlanmamış atamaları getirir
        /// </summary>
        public async Task<List<SinavAtama>> GetPendingByUserAsync(int kullaniciId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavAtama
                    .Include(a => a.Kullanici)
                    .Include(a => a.Kullanici1)
                    .Include(a => a.Oturum)
                    .Where(a => a.KullaniciID == kullaniciId && !a.TamamlandiMi)
                    .Where(a => !a.SonGecerlilikTarihi.HasValue || a.SonGecerlilikTarihi > DateTime.UtcNow)
                    .OrderByDescending(a => a.AtamaTarihi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Atamayı ID'ye göre getirir
        /// </summary>
        public async Task<SinavAtama> GetByIdAsync(int atamaId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavAtama
                    .Include(a => a.Kullanici)
                    .Include(a => a.Kullanici1)
                    .Include(a => a.Oturum)
                    .FirstOrDefaultAsync(a => a.AtamaID == atamaId);
            }
        }

        /// <summary>
        /// Oturuma atanan sınavı getirir (YENİ)
        /// </summary>
        public async Task<SinavAtama> GetBySessionAsync(int oturumId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavAtama
                    .Include(a => a.Oturum)
                    .Include(a => a.Kullanici1) // Atayan yönetici
                    .FirstOrDefaultAsync(a => a.OturumID == oturumId);
            }
        }

        /// <summary>
        /// Kullanıcının oturumlarına atanan sınavları getirir (YENİ)
        /// </summary>
        public async Task<List<SinavAtama>> GetByUserSessionsAsync(int kullaniciId)
        {
            using (var context = DbContextFactory.Create())
            {
                // Önce kullanıcının oturum ID'lerini al
                var oturumIds = await context.Oturum
                    .Where(o => o.KullaniciID == kullaniciId)
                    .Select(o => o.OturumID)
                    .ToListAsync();

                // Sonra bu oturumlara atanan sınavları getir
                return await context.SinavAtama
                    .Include(a => a.Oturum)
                    .Include(a => a.Kullanici1)
                    .Where(a => a.OturumID != null && oturumIds.Contains(a.OturumID.Value))
                    .OrderByDescending(a => a.AtamaTarihi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Atamayı tamamlandı olarak işaretler
        /// </summary>
        public async Task MarkAsCompletedAsync(int atamaId)
        {
            using (var context = DbContextFactory.Create())
            {
                var atama = await context.SinavAtama.FindAsync(atamaId);
                if (atama != null)
                {
                    atama.TamamlandiMi = true;
                    atama.TamamlanmaTarihi = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Atamayı günceller
        /// </summary>
        public async Task UpdateAsync(SinavAtama atama)
        {
            using (var context = DbContextFactory.Create())
            {
                var existing = await context.SinavAtama.FindAsync(atama.AtamaID);
                if (existing != null)
                {
                    existing.SinavAdi = atama.SinavAdi;
                    existing.SinavAciklama = atama.SinavAciklama;
                    existing.SinavJsonPath = atama.SinavJsonPath;
                    existing.SinavJsonContent = atama.SinavJsonContent;
                    existing.SonGecerlilikTarihi = atama.SonGecerlilikTarihi;
                    existing.Notlar = atama.Notlar;
                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Atamayı siler
        /// </summary>
        public async Task DeleteAsync(int atamaId)
        {
            using (var context = DbContextFactory.Create())
            {
                var atama = await context.SinavAtama.FindAsync(atamaId);
                if (atama != null)
                {
                    context.SinavAtama.Remove(atama);
                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Tüm atamaları getirir (Yönetici için)
        /// </summary>
        public async Task<List<SinavAtama>> GetAllAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavAtama
                    .Include(a => a.Kullanici)
                    .Include(a => a.Kullanici1)
                    .Include(a => a.Oturum)
                    .OrderByDescending(a => a.AtamaTarihi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Belirli bir yöneticinin yaptığı atamaları getirir
        /// </summary>
        public async Task<List<SinavAtama>> GetByManagerAsync(int yoneticiId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavAtama
                    .Include(a => a.Kullanici)
                    .Include(a => a.Kullanici1)
                    .Include(a => a.Oturum)
                    .Where(a => a.AtayanYoneticiID == yoneticiId)
                    .OrderByDescending(a => a.AtamaTarihi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Deney grubundaki tüm kullanıcılara toplu sınav atar
        /// </summary>
        public async Task<int> CreateForGroupAsync(
            int grupId,
            string sinavAdi,
            string sinavAciklama,
            string sinavJsonPath,
            string sinavJsonContent,
            int atayanYoneticiId,
            string notlar = null)
        {
            using (var context = DbContextFactory.Create())
            {
                // Gruptaki tüm aktif kullanıcıları bul
                var kullanicilar = await context.Kullanici
                    .Where(k => k.DeneyGrubuID == grupId)
                    .ToListAsync();

                if (kullanicilar.Count == 0)
                {
                    return 0;
                }

                int atamaCount = 0;
                foreach (var kullanici in kullanicilar)
                {
                    // Aynı sınav zaten atanmış mı kontrol et
                    var mevcutAtama = await context.SinavAtama
                        .FirstOrDefaultAsync(a => a.KullaniciID == kullanici.KullaniciID 
                            && a.SinavAdi == sinavAdi 
                            && !a.TamamlandiMi);

                    if (mevcutAtama != null)
                    {
                        // Zaten atanmış, atla
                        continue;
                    }

                    var atama = new SinavAtama
                    {
                        KullaniciID = kullanici.KullaniciID,
                        OturumID = null, // Genel atama
                        SinavAdi = sinavAdi,
                        SinavAciklama = sinavAciklama,
                        SinavJsonPath = sinavJsonPath,
                        SinavJsonContent = sinavJsonContent,
                        AtayanYoneticiID = atayanYoneticiId,
                        AtamaTarihi = DateTime.UtcNow,
                        SonGecerlilikTarihi = null,
                        TamamlandiMi = false,
                        Notlar = notlar
                    };

                    context.SinavAtama.Add(atama);
                    atamaCount++;
                }

                await context.SaveChangesAsync();
                return atamaCount;
            }
        }
    }
}
