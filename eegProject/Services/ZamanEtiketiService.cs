using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    internal sealed class ZamanEtiketiService
    {
        /// <summary>
        /// Tüm zaman etiketlerini getirir (aktif + pasif)
        /// </summary>
        public async Task<List<ZamanEtiketi>> GetAllAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.ZamanEtiketi
                    .OrderBy(z => z.EtiketAdi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Sadece aktif zaman etiketlerini getirir
        /// </summary>
        public async Task<List<ZamanEtiketi>> GetActiveAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.ZamanEtiketi
                    .Where(z => z.Aktif)
                    .OrderBy(z => z.EtiketAdi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// ID'ye göre zaman etiketi getirir
        /// </summary>
        public async Task<ZamanEtiketi> GetByIdAsync(int id)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.ZamanEtiketi
                    .FirstOrDefaultAsync(z => z.ZamanEtiketiID == id);
            }
        }

        /// <summary>
        /// Etiket adına göre zaman etiketi arar (case-insensitive)
        /// </summary>
        public async Task<ZamanEtiketi> GetByNameAsync(string etiketAdi)
        {
            if (string.IsNullOrWhiteSpace(etiketAdi))
            {
                return null;
            }

            using (var context = DbContextFactory.Create())
            {
                var normalizedName = etiketAdi.Trim();
                return await context.ZamanEtiketi
                    .FirstOrDefaultAsync(z => z.EtiketAdi == normalizedName);
            }
        }

        /// <summary>
        /// Yeni zaman etiketi oluşturur
        /// </summary>
        public async Task<ZamanEtiketi> CreateAsync(string etiketAdi, string aciklama = null)
        {
            if (string.IsNullOrWhiteSpace(etiketAdi))
            {
                throw new ArgumentException("Etiket adı boş olamaz", nameof(etiketAdi));
            }

            using (var context = DbContextFactory.Create())
            {
                var normalizedName = etiketAdi.Trim();

                // Aynı isimde kayıt var mı kontrol et
                var existing = await context.ZamanEtiketi
                    .FirstOrDefaultAsync(z => z.EtiketAdi == normalizedName);

                if (existing != null)
                {
                    throw new InvalidOperationException($"'{normalizedName}' adlı zaman etiketi zaten mevcut.");
                }

                var zamanEtiketi = new ZamanEtiketi
                {
                    EtiketAdi = normalizedName,
                    Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                    Aktif = true,
                    EklenmeTarihi = DateTime.Now
                };

                context.ZamanEtiketi.Add(zamanEtiketi);
                await context.SaveChangesAsync();
                return zamanEtiketi;
            }
        }

        /// <summary>
        /// Mevcut zaman etiketini günceller
        /// </summary>
        public async Task UpdateAsync(int id, string etiketAdi, string aciklama = null)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(etiketAdi))
            {
                throw new ArgumentException("Etiket adı boş olamaz", nameof(etiketAdi));
            }

            using (var context = DbContextFactory.Create())
            {
                var zamanEtiketi = await context.ZamanEtiketi
                    .FirstOrDefaultAsync(z => z.ZamanEtiketiID == id);

                if (zamanEtiketi == null)
                {
                    throw new InvalidOperationException("Zaman etiketi bulunamadı.");
                }

                var normalizedName = etiketAdi.Trim();

                // Aynı isimde başka kayıt var mı kontrol et
                var duplicate = await context.ZamanEtiketi
                    .FirstOrDefaultAsync(z => z.EtiketAdi == normalizedName && z.ZamanEtiketiID != id);

                if (duplicate != null)
                {
                    throw new InvalidOperationException($"'{normalizedName}' adlı zaman etiketi zaten mevcut.");
                }

                zamanEtiketi.EtiketAdi = normalizedName;
                zamanEtiketi.Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim();

                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Zaman etiketini siler (veya kullanılıyorsa pasif yapar)
        /// </summary>
        public async Task<bool> DeleteAsync(int id, bool forceDelete = false)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            using (var context = DbContextFactory.Create())
            {
                var zamanEtiketi = await context.ZamanEtiketi
                    .Include(z => z.Oturum)
                    .FirstOrDefaultAsync(z => z.ZamanEtiketiID == id);

                if (zamanEtiketi == null)
                {
                    return false;
                }

                // Kullanımda mı?
                var isUsed = zamanEtiketi.Oturum.Any();

                if (isUsed && !forceDelete)
                {
                    // Pasif yap (soft delete)
                    zamanEtiketi.Aktif = false;
                    await context.SaveChangesAsync();
                    return true;
                }

                if (!isUsed)
                {
                    // Gerçekten sil (hard delete)
                    context.ZamanEtiketi.Remove(zamanEtiketi);
                    await context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Bu zaman etiketinin kaç oturumda kullanıldığını döner
        /// </summary>
        public async Task<int> GetUsageCountAsync(int id)
        {
            if (id <= 0)
            {
                return 0;
            }

            using (var context = DbContextFactory.Create())
            {
                return await context.Oturum
                    .CountAsync(o => o.ZamanEtiketiID == id);
            }
        }

        /// <summary>
        /// Aktif/Pasif durumu değiştirir
        /// </summary>
        public async Task SetActiveAsync(int id, bool aktif)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            using (var context = DbContextFactory.Create())
            {
                var zamanEtiketi = await context.ZamanEtiketi
                    .FirstOrDefaultAsync(z => z.ZamanEtiketiID == id);

                if (zamanEtiketi == null)
                {
                    throw new InvalidOperationException("Zaman etiketi bulunamadı.");
                }

                zamanEtiketi.Aktif = aktif;
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Eğer mevcut değilse yeni zaman etiketi oluşturur, varsa mevcut kaydı döner
        /// </summary>
        public async Task<ZamanEtiketi> GetOrCreateAsync(string etiketAdi, string aciklama = null)
        {
            if (string.IsNullOrWhiteSpace(etiketAdi))
            {
                return null;
            }

            var existing = await GetByNameAsync(etiketAdi);
            if (existing != null)
            {
                return existing;
            }

            return await CreateAsync(etiketAdi, aciklama);
        }
    }
}


