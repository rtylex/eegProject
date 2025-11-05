using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    public class ModulYetkisiService
    {
        /// <summary>
        /// Kullanıcının belirli bir modüle erişim yetkisi var mı kontrol eder
        /// </summary>
        public async Task<bool> HasModuleAccessAsync(int kullaniciId, string modulAdi)
        {
            using (var context = DbContextFactory.Create())
            {
                var yetki = await context.KullaniciModulYetkisi
                    .FirstOrDefaultAsync(y => 
                        y.KullaniciID == kullaniciId && 
                        y.ModulAdi == modulAdi && 
                        y.AktifMi);

                return yetki != null;
            }
        }

        /// <summary>
        /// Kullanıcıya modül yetkisi tanımlar veya günceller
        /// </summary>
        public async Task SetModuleAccessAsync(int kullaniciId, string modulAdi, bool aktifMi, int tanimlayanYoneticiId)
        {
            using (var context = DbContextFactory.Create())
            {
                var mevcutYetki = await context.KullaniciModulYetkisi
                    .FirstOrDefaultAsync(y => 
                        y.KullaniciID == kullaniciId && 
                        y.ModulAdi == modulAdi);

                if (mevcutYetki != null)
                {
                    // Mevcut yetki var - güncelle
                    mevcutYetki.AktifMi = aktifMi;
                    mevcutYetki.TanimlayanYoneticiID = tanimlayanYoneticiId;
                }
                else if (aktifMi)
                {
                    // Yeni yetki ekle (sadece aktif ise)
                    var yeniYetki = new KullaniciModulYetkisi
                    {
                        KullaniciID = kullaniciId,
                        ModulAdi = modulAdi,
                        AktifMi = true,
                        TanimTarihi = DateTime.Now,
                        TanimlayanYoneticiID = tanimlayanYoneticiId
                    };
                    context.KullaniciModulYetkisi.Add(yeniYetki);
                }

                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Tüm kullanıcıların belirli bir modül için yetki durumlarını getirir
        /// </summary>
        public async Task<Dictionary<int, bool>> GetModuleAccessForAllUsersAsync(string modulAdi)
        {
            using (var context = DbContextFactory.Create())
            {
                var yetkiler = await context.KullaniciModulYetkisi
                    .Where(y => y.ModulAdi == modulAdi && y.AktifMi)
                    .Select(y => y.KullaniciID)
                    .ToListAsync();

                return yetkiler.ToDictionary(id => id, id => true);
            }
        }

        /// <summary>
        /// Kullanıcının tüm modül yetkilerini getirir
        /// </summary>
        public async Task<List<KullaniciModulYetkisi>> GetUserModuleAccessesAsync(int kullaniciId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.KullaniciModulYetkisi
                    .Where(y => y.KullaniciID == kullaniciId && y.AktifMi)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Belirli bir modülü kullanan tüm kullanıcıları getirir
        /// </summary>
        public async Task<List<Kullanici>> GetUsersWithModuleAccessAsync(string modulAdi)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.KullaniciModulYetkisi
                    .Where(y => y.ModulAdi == modulAdi && y.AktifMi)
                    .Select(y => y.Kullanici)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Kullanıcının modül yetkisini kaldırır
        /// </summary>
        public async Task RemoveModuleAccessAsync(int kullaniciId, string modulAdi)
        {
            using (var context = DbContextFactory.Create())
            {
                var yetki = await context.KullaniciModulYetkisi
                    .FirstOrDefaultAsync(y => 
                        y.KullaniciID == kullaniciId && 
                        y.ModulAdi == modulAdi);

                if (yetki != null)
                {
                    yetki.AktifMi = false;
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}

