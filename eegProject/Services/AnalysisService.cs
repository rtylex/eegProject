using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    internal sealed class AnalysisService
    {
        public async Task<List<AnalizSonucu>> GetRecentAsync(int take = 100)
        {
            if (take <= 0)
            {
                take = 100;
            }

            using (var context = DbContextFactory.Create())
            {
                return await context.AnalizSonucu
                    .Include(a => a.Oturum)
                    .OrderByDescending(a => a.AnalizTarihi)
                    .Take(take)
                    .ToListAsync();
            }
        }

        public async Task<List<AnalizSonucu>> GetBySessionAsync(int sessionId)
        {
            if (sessionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId));
            }

            using (var context = DbContextFactory.Create())
            {
                return await context.AnalizSonucu
                    .Where(a => a.OturumID == sessionId)
                    .OrderByDescending(a => a.AnalizTarihi)
                    .ToListAsync();
            }
        }

        public async Task<AnalizSonucu> CreateAsync(AnalizSonucu result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            result.AnalizTarihi = result.AnalizTarihi == default ? DateTime.UtcNow : result.AnalizTarihi;

            using (var context = DbContextFactory.Create())
            {
                context.AnalizSonucu.Add(result);
                await context.SaveChangesAsync();
                return result;
            }
        }

        public async Task DeleteAsync(int analysisId)
        {
            if (analysisId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(analysisId));
            }

            using (var context = DbContextFactory.Create())
            {
                var result = await context.AnalizSonucu.FirstOrDefaultAsync(a => a.AnalizID == analysisId);
                if (result == null)
                {
                    return;
                }

                context.AnalizSonucu.Remove(result);
                await context.SaveChangesAsync();
            }
        }
    }
}

