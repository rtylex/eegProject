using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    internal sealed class UserService
    {
        public async Task<List<Kullanici>> GetAllAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.Kullanici
                    .OrderBy(u => u.AdSoyad)
                    .ToListAsync();
            }
        }

        public async Task<Kullanici> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            using (var context = DbContextFactory.Create())
            {
                return await context.Kullanici
                    .FirstOrDefaultAsync(u => u.Email == email.Trim());
            }
        }

        public async Task<Kullanici> CreateAsync(string adSoyad, string email, string passwordHash, string role, int? deneyGrubuId = null)
        {
            if (string.IsNullOrWhiteSpace(adSoyad))
            {
                throw new ArgumentException("AdSoyad is required", nameof(adSoyad));
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("Password hash is required", nameof(passwordHash));
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                role = "Kullanici";
            }

            using (var context = DbContextFactory.Create())
            {
                var now = DateTime.UtcNow;
                var user = new Kullanici
                {
                    AdSoyad = adSoyad.Trim(),
                    Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                    SifreHash = passwordHash,
                    KayitTarihi = now,
                    Rol = role.Trim(),
                    DeneyGrubuID = deneyGrubuId
                };

                context.Kullanici.Add(user);
                await context.SaveChangesAsync();
                return user;
            }
        }

        public async Task UpdateAsync(int userId, string adSoyad, string email, string role, int? deneyGrubuId = null)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var context = DbContextFactory.Create())
            {
                var user = await context.Kullanici.FirstOrDefaultAsync(u => u.KullaniciID == userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                if (!string.IsNullOrWhiteSpace(adSoyad))
                {
                    user.AdSoyad = adSoyad.Trim();
                }

                user.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
                user.Rol = string.IsNullOrWhiteSpace(role) ? user.Rol : role.Trim();
                user.DeneyGrubuID = deneyGrubuId;

                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var context = DbContextFactory.Create())
            {
                // Transaction ile güvenli silme
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        var user = await context.Kullanici
                            .Include(u => u.Oturum)
                            .Include(u => u.EEGVerisi)
                            .Include(u => u.KullaniciModulYetkisi)
                            .FirstOrDefaultAsync(u => u.KullaniciID == userId);

                        if (user == null)
                        {
                            return;
                        }

                        // 1. Önce Oturumlara bağlı SinavSonucu kayıtlarını sil
                        var sessionIds = user.Oturum.Select(o => o.OturumID).ToList();
                        if (sessionIds.Any())
                        {
                            var examResults = await context.SinavSonucu
                                .Where(ss => sessionIds.Contains(ss.OturumID))
                                .ToListAsync();
                            context.SinavSonucu.RemoveRange(examResults);
                        }

                        // 2. Oturumlara bağlı AnalizSonucu kayıtlarını sil
                        if (sessionIds.Any())
                        {
                            var analysisResults = await context.AnalizSonucu
                                .Where(a => a.OturumID.HasValue && sessionIds.Contains(a.OturumID.Value))
                                .ToListAsync();
                            context.AnalizSonucu.RemoveRange(analysisResults);
                        }

                        // 3. EEGVerisi kayıtlarını sil
                        if (user.EEGVerisi.Any())
                        {
                            context.EEGVerisi.RemoveRange(user.EEGVerisi);
                        }

                        // 4. Oturum kayıtlarını sil
                        if (user.Oturum.Any())
                        {
                            context.Oturum.RemoveRange(user.Oturum);
                        }

                        // 5. Modül yetkileri kayıtlarını sil
                        if (user.KullaniciModulYetkisi.Any())
                        {
                            context.KullaniciModulYetkisi.RemoveRange(user.KullaniciModulYetkisi);
                        }

                        // 6. AuditLog kayıtları (opsiyonel - silinebilir veya korunabilir)
                        // Şimdilik AuditLog'ları koruyoruz (audit trail için)
                        // İsterseniz bunları da silebiliriz:
                        // var auditLogs = await context.AuditLog
                        //     .Where(a => a.KullaniciID == userId)
                        //     .ToListAsync();
                        // context.AuditLog.RemoveRange(auditLogs);

                        // 7. Son olarak kullanıcıyı sil
                        context.Kullanici.Remove(user);

                        await context.SaveChangesAsync();
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task ResetPasswordAsync(int userId, string newPasswordHash)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(newPasswordHash))
            {
                throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));
            }

            using (var context = DbContextFactory.Create())
            {
                var user = await context.Kullanici.FirstOrDefaultAsync(u => u.KullaniciID == userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                user.SifreHash = newPasswordHash;
                await context.SaveChangesAsync();
            }
        }

        public async Task UpdateUserNotesAsync(int userId, string notes)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var context = DbContextFactory.Create())
            {
                var user = await context.Kullanici.FirstOrDefaultAsync(u => u.KullaniciID == userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                user.Notlar = notes;
                await context.SaveChangesAsync();
            }
        }
    }
}
