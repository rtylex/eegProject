using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    internal sealed class ExamService
    {
        public async Task<SinavSonucu> CreateAsync(SinavSonucu result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            using (var context = DbContextFactory.Create())
            {
                context.SinavSonucu.Add(result);
                await context.SaveChangesAsync();
                return result;
            }
        }

        public async Task<List<SinavSonucu>> GetBySessionAsync(int sessionId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavSonucu
                    .Where(s => s.OturumID == sessionId)
                    .OrderByDescending(s => s.BaslamaTarihi)
                    .ToListAsync();
            }
        }

        public async Task<List<SinavSonucu>> GetByUserAsync(int userId)
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.SinavSonucu
                    .Include(s => s.Oturum)
                    .Where(s => s.Oturum.KullaniciID == userId)
                    .OrderByDescending(s => s.BaslamaTarihi)
                    .ToListAsync();
            }
        }

        public async Task DeleteAsync(int examResultId)
        {
            using (var context = DbContextFactory.Create())
            {
                var result = await context.SinavSonucu
                    .FirstOrDefaultAsync(s => s.SinavSonucuID == examResultId);
                if (result == null) return;

                context.SinavSonucu.Remove(result);
                await context.SaveChangesAsync();
            }
        }
    }
}



