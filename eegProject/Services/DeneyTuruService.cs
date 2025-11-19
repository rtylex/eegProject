using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    internal sealed class DeneyTuruService
    {
        /// <summary>
        /// Tüm deney türlerini getirir (aktif + pasif)
        /// </summary>
        public async Task<List<DeneyTuru>> GetAllAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.DeneyTuru
                    .OrderBy(d => d.TurAdi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Sadece aktif deney türlerini getirir
        /// </summary>
        public async Task<List<DeneyTuru>> GetActiveAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.DeneyTuru
                    .Where(d => d.Aktif)
                    .OrderBy(d => d.TurAdi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// ID'ye göre deney türü getirir
        /// </summary>
        public async Task<DeneyTuru> GetByIdAsync(int id)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.DeneyTuru
                    .FirstOrDefaultAsync(d => d.DeneyTuruID == id);
            }
        }

        /// <summary>
        /// Tür adına göre deney türü arar (case-insensitive)
        /// </summary>
        public async Task<DeneyTuru> GetByNameAsync(string turAdi)
        {
            if (string.IsNullOrWhiteSpace(turAdi))
            {
                return null;
            }

            using (var context = DbContextFactory.Create())
            {
                var normalizedName = turAdi.Trim();
                return await context.DeneyTuru
                    .FirstOrDefaultAsync(d => d.TurAdi == normalizedName);
            }
        }

        /// <summary>
        /// Yeni deney türü oluşturur
        /// </summary>
        public async Task<DeneyTuru> CreateAsync(string turAdi, string aciklama = null)
        {
            if (string.IsNullOrWhiteSpace(turAdi))
            {
                throw new ArgumentException("Tür adı boş olamaz", nameof(turAdi));
            }

            using (var context = DbContextFactory.Create())
            {
                var normalizedName = turAdi.Trim();

                // Aynı isimde kayıt var mı kontrol et
                var existing = await context.DeneyTuru
                    .FirstOrDefaultAsync(d => d.TurAdi == normalizedName);

                if (existing != null)
                {
                    throw new InvalidOperationException($"'{normalizedName}' adlı deney türü zaten mevcut.");
                }

                var deneyTuru = new DeneyTuru
                {
                    TurAdi = normalizedName,
                    Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                    Aktif = true,
                    EklenmeTarihi = DateTime.Now
                };

                context.DeneyTuru.Add(deneyTuru);
                await context.SaveChangesAsync();
                return deneyTuru;
            }
        }

        /// <summary>
        /// Mevcut deney türünü günceller
        /// </summary>
        public async Task UpdateAsync(int id, string turAdi, string aciklama = null)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(turAdi))
            {
                throw new ArgumentException("Tür adı boş olamaz", nameof(turAdi));
            }

            using (var context = DbContextFactory.Create())
            {
                var deneyTuru = await context.DeneyTuru
                    .FirstOrDefaultAsync(d => d.DeneyTuruID == id);

                if (deneyTuru == null)
                {
                    throw new InvalidOperationException("Deney türü bulunamadı.");
                }

                var normalizedName = turAdi.Trim();

                // Aynı isimde başka kayıt var mı kontrol et
                var duplicate = await context.DeneyTuru
                    .FirstOrDefaultAsync(d => d.TurAdi == normalizedName && d.DeneyTuruID != id);

                if (duplicate != null)
                {
                    throw new InvalidOperationException($"'{normalizedName}' adlı deney türü zaten mevcut.");
                }

                deneyTuru.TurAdi = normalizedName;
                deneyTuru.Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim();

                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deney türünü siler (veya kullanılıyorsa pasif yapar)
        /// </summary>
        public async Task<bool> DeleteAsync(int id, bool forceDelete = false)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            using (var context = DbContextFactory.Create())
            {
                var deneyTuru = await context.DeneyTuru
                    .Include(d => d.Oturum)
                    .FirstOrDefaultAsync(d => d.DeneyTuruID == id);

                if (deneyTuru == null)
                {
                    return false;
                }

                // Kullanımda mı?
                var isUsed = deneyTuru.Oturum.Any();

                if (isUsed && !forceDelete)
                {
                    // Pasif yap (soft delete)
                    deneyTuru.Aktif = false;
                    await context.SaveChangesAsync();
                    return true;
                }

                if (!isUsed)
                {
                    // Gerçekten sil (hard delete)
                    context.DeneyTuru.Remove(deneyTuru);
                    await context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Bu deney türünün kaç oturumda kullanıldığını döner
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
                    .CountAsync(o => o.DeneyTuruID == id);
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
                var deneyTuru = await context.DeneyTuru
                    .FirstOrDefaultAsync(d => d.DeneyTuruID == id);

                if (deneyTuru == null)
                {
                    throw new InvalidOperationException("Deney türü bulunamadı.");
                }

                deneyTuru.Aktif = aktif;
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Eğer mevcut değilse yeni deney türü oluşturur, varsa mevcut kaydı döner
        /// </summary>
        public async Task<DeneyTuru> GetOrCreateAsync(string turAdi, string aciklama = null)
        {
            if (string.IsNullOrWhiteSpace(turAdi))
            {
                return null;
            }

            var existing = await GetByNameAsync(turAdi);
            if (existing != null)
            {
                return existing;
            }

            return await CreateAsync(turAdi, aciklama);
        }
    }
}








