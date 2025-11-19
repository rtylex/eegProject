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
        /// Kullanıcıya sınav atar
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
        /// Kullanıcının atanmış sınavlarını getirir
        /// </summary>
        public async Task<List<SinavAtama>> GetByUserAsync(int kullaniciId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavAtama
                    .Include(a => a.Kullanici)
                    .Include(a => a.Kullanici1) // AtayanYonetici
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
                    .Where(a => a.KullaniciID == kullaniciId && !a.TamamlandiMi)
                    .Where(a => !a.SonGecerlilikTarihi.HasValue || a.SonGecerlilikTarihi > DateTime.UtcNow)
                    .OrderByDescending(a => a.AtamaTarihi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Yöneticinin yaptığı atamaları getirir
        /// </summary>
        public async Task<List<SinavAtama>> GetByManagerAsync(int yoneticiId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavAtama
                    .Include(a => a.Kullanici)
                    .Where(a => a.AtayanYoneticiID == yoneticiId)
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
                    .FirstOrDefaultAsync(a => a.AtamaID == atamaId);
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
                    .OrderByDescending(a => a.AtamaTarihi)
                    .ToListAsync();
            }
        }
    }
}
