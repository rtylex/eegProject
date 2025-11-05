using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    internal sealed class AuditLogService
    {
        public async Task LogAsync(string islem, string detay = null, int? kullaniciId = null, string kullaniciAdi = null, string seviye = "Info")
        {
            try
            {
                using (var context = DbContextFactory.Create())
                {
                    var log = new AuditLog
                    {
                        Tarih = DateTime.UtcNow,
                        KullaniciID = kullaniciId,
                        KullaniciAdi = kullaniciAdi,
                        Islem = islem,
                        Detay = detay,
                        Seviye = seviye ?? "Info"
                    };

                    context.AuditLog.Add(log);
                    await context.SaveChangesAsync();
                }
            }
            catch
            {
                // Loglama hatası sessizce yutulur (sonsuz döngü önlemek için)
            }
        }

        public async Task<List<AuditLog>> GetRecentAsync(int count = 500)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.AuditLog
                    .OrderByDescending(l => l.Tarih)
                    .Take(count)
                    .ToListAsync();
            }
        }

        public async Task<List<AuditLog>> GetByUserAsync(int kullaniciId, int count = 100)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.AuditLog
                    .Where(l => l.KullaniciID == kullaniciId)
                    .OrderByDescending(l => l.Tarih)
                    .Take(count)
                    .ToListAsync();
            }
        }

        public async Task<List<AuditLog>> GetByDateRangeAsync(DateTime baslangic, DateTime bitis)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.AuditLog
                    .Where(l => l.Tarih >= baslangic && l.Tarih <= bitis)
                    .OrderByDescending(l => l.Tarih)
                    .ToListAsync();
            }
        }

        public async Task<List<AuditLog>> GetByLevelAsync(string seviye, int count = 500)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.AuditLog
                    .Where(l => l.Seviye == seviye)
                    .OrderByDescending(l => l.Tarih)
                    .Take(count)
                    .ToListAsync();
            }
        }

        public async Task DeleteAllAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                // Tüm logları sil
                var allLogs = await context.AuditLog.ToListAsync();
                context.AuditLog.RemoveRange(allLogs);
                await context.SaveChangesAsync();
            }
        }
    }
}

