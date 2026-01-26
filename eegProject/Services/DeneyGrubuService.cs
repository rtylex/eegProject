using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    /// <summary>
    /// Deney grubu yönetim servisi
    /// NeuroIS protokolü için deney/kontrol grubu ayrımı sağlar
    /// </summary>
    internal sealed class DeneyGrubuService
    {
        /// <summary>
        /// Tüm deney gruplarını getirir (aktif + pasif)
        /// </summary>
        public async Task<List<DeneyGrubu>> GetAllAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.DeneyGrubu
                    .OrderBy(g => g.GrupAdi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Sadece aktif deney gruplarını getirir
        /// </summary>
        public async Task<List<DeneyGrubu>> GetActiveAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.DeneyGrubu
                    .Where(g => g.Aktif)
                    .OrderBy(g => g.GrupAdi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// ID'ye göre deney grubu getirir
        /// </summary>
        public async Task<DeneyGrubu> GetByIdAsync(int id)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.DeneyGrubu
                    .FirstOrDefaultAsync(g => g.DeneyGrubuID == id);
            }
        }

        /// <summary>
        /// Grup adına göre deney grubu arar (case-insensitive)
        /// </summary>
        public async Task<DeneyGrubu> GetByNameAsync(string grupAdi)
        {
            if (string.IsNullOrWhiteSpace(grupAdi))
            {
                return null;
            }

            using (var context = DbContextFactory.Create())
            {
                var normalizedName = grupAdi.Trim();
                return await context.DeneyGrubu
                    .FirstOrDefaultAsync(g => g.GrupAdi == normalizedName);
            }
        }

        /// <summary>
        /// Yeni deney grubu oluşturur
        /// </summary>
        public async Task<DeneyGrubu> CreateAsync(string grupAdi, string aciklama = null)
        {
            if (string.IsNullOrWhiteSpace(grupAdi))
            {
                throw new ArgumentException("Grup adı boş olamaz", nameof(grupAdi));
            }

            using (var context = DbContextFactory.Create())
            {
                var normalizedName = grupAdi.Trim();

                // Aynı isimde kayıt var mı kontrol et
                var existing = await context.DeneyGrubu
                    .FirstOrDefaultAsync(g => g.GrupAdi == normalizedName);

                if (existing != null)
                {
                    throw new InvalidOperationException($"'{normalizedName}' adlı deney grubu zaten mevcut.");
                }

                var deneyGrubu = new DeneyGrubu
                {
                    GrupAdi = normalizedName,
                    Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                    Aktif = true,
                    EklenmeTarihi = DateTime.Now
                };

                context.DeneyGrubu.Add(deneyGrubu);
                await context.SaveChangesAsync();
                return deneyGrubu;
            }
        }

        /// <summary>
        /// Mevcut deney grubunu günceller
        /// </summary>
        public async Task UpdateAsync(int id, string grupAdi, string aciklama = null)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(grupAdi))
            {
                throw new ArgumentException("Grup adı boş olamaz", nameof(grupAdi));
            }

            using (var context = DbContextFactory.Create())
            {
                var deneyGrubu = await context.DeneyGrubu
                    .FirstOrDefaultAsync(g => g.DeneyGrubuID == id);

                if (deneyGrubu == null)
                {
                    throw new InvalidOperationException("Deney grubu bulunamadı.");
                }

                var normalizedName = grupAdi.Trim();

                // Aynı isimde başka kayıt var mı kontrol et
                var duplicate = await context.DeneyGrubu
                    .FirstOrDefaultAsync(g => g.GrupAdi == normalizedName && g.DeneyGrubuID != id);

                if (duplicate != null)
                {
                    throw new InvalidOperationException($"'{normalizedName}' adlı deney grubu zaten mevcut.");
                }

                deneyGrubu.GrupAdi = normalizedName;
                deneyGrubu.Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim();

                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deney grubunu siler (veya kullanılıyorsa pasif yapar)
        /// </summary>
        public async Task<bool> DeleteAsync(int id, bool forceDelete = false)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            using (var context = DbContextFactory.Create())
            {
                var deneyGrubu = await context.DeneyGrubu
                    .Include(g => g.Kullanici)
                    .FirstOrDefaultAsync(g => g.DeneyGrubuID == id);

                if (deneyGrubu == null)
                {
                    return false;
                }

                // Kullanımda mı?
                var isUsed = deneyGrubu.Kullanici.Any();

                if (isUsed && !forceDelete)
                {
                    // Pasif yap (soft delete)
                    deneyGrubu.Aktif = false;
                    await context.SaveChangesAsync();
                    return true;
                }

                if (!isUsed)
                {
                    // Gerçekten sil (hard delete)
                    context.DeneyGrubu.Remove(deneyGrubu);
                    await context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Bu deney grubuna kaç kullanıcı atandığını döner
        /// </summary>
        public async Task<int> GetUsageCountAsync(int id)
        {
            if (id <= 0)
            {
                return 0;
            }

            using (var context = DbContextFactory.Create())
            {
                return await context.Kullanici
                    .CountAsync(k => k.DeneyGrubuID == id);
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
                var deneyGrubu = await context.DeneyGrubu
                    .FirstOrDefaultAsync(g => g.DeneyGrubuID == id);

                if (deneyGrubu == null)
                {
                    throw new InvalidOperationException("Deney grubu bulunamadı.");
                }

                deneyGrubu.Aktif = aktif;
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Eğer mevcut değilse yeni deney grubu oluşturur, varsa mevcut kaydı döner
        /// </summary>
        public async Task<DeneyGrubu> GetOrCreateAsync(string grupAdi, string aciklama = null)
        {
            if (string.IsNullOrWhiteSpace(grupAdi))
            {
                return null;
            }

            var existing = await GetByNameAsync(grupAdi);
            if (existing != null)
            {
                return existing;
            }

            return await CreateAsync(grupAdi, aciklama);
        }

        /// <summary>
        /// Belirli bir gruptaki tüm kullanıcıların oturumlarını getirir
        /// Grup karşılaştırma analizi için kullanılır
        /// </summary>
        public async Task<List<int>> GetSessionIdsByGroupAsync(int grupId)
        {
            if (grupId <= 0)
            {
                return new List<int>();
            }

            using (var context = DbContextFactory.Create())
            {
                return await context.Oturum
                    .Where(o => o.Kullanici.DeneyGrubuID == grupId)
                    .Select(o => o.OturumID)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Belirli bir gruptaki ve belirli oturum tipindeki oturumları getirir
        /// Örn: "Yapay Zeka" grubunun "Görev" oturumları
        /// </summary>
        public async Task<List<int>> GetSessionIdsByGroupAndTypeAsync(int grupId, string oturumTipi = null)
        {
            if (grupId <= 0)
            {
                return new List<int>();
            }

            using (var context = DbContextFactory.Create())
            {
                var query = context.Oturum
                    .Where(o => o.Kullanici.DeneyGrubuID == grupId);

                if (!string.IsNullOrWhiteSpace(oturumTipi))
                {
                    query = query.Where(o => o.OturumTipi == oturumTipi);
                }

                return await query
                    .Select(o => o.OturumID)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Belirli bir gruptaki tüm kullanıcıların oturumlarını tam obje olarak getirir
        /// </summary>
        public async Task<List<Oturum>> GetSessionsByGroupAsync(int grupId)
        {
            if (grupId <= 0)
            {
                return new List<Oturum>();
            }

            using (var context = DbContextFactory.Create())
            {
                return await context.Oturum
                    .Where(o => o.Kullanici.DeneyGrubuID == grupId)
                    .ToListAsync();
            }
        }
    }
}
