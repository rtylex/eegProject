using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    internal sealed class EegDataService
    {
        public async Task<List<EEGVerisi>> GetRecentBySessionAsync(int sessionId, int take = 500)
        {
            if (sessionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId));
            }

            if (take <= 0)
            {
                take = 500;
            }

            using (var context = DbContextFactory.Create())
            {
                var rows = await context.EEGVerisi
                    .Where(v => v.OturumID == sessionId)
                    .OrderByDescending(v => v.KayitZamani)
                    .Take(take)
                    .ToListAsync();

                rows.Reverse();
                return rows;
            }
        }

        public async Task<List<EEGVerisi>> GetByUserAsync(int userId, DateTime? from = null, DateTime? to = null)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var context = DbContextFactory.Create())
            {
                var query = context.EEGVerisi.Where(v => v.KullaniciID == userId);

                if (from.HasValue)
                {
                    query = query.Where(v => v.KayitZamani >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(v => v.KayitZamani <= to.Value);
                }

                return await query
                    .OrderBy(v => v.KayitZamani)
                    .Take(5000)
                    .ToListAsync();
            }
        }

        public async Task<EEGVerisi> InsertAsync(EEGVerisi sample)
        {
            if (sample == null)
            {
                throw new ArgumentNullException(nameof(sample));
            }

            if (sample.OturumID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(EEGVerisi.OturumID), "OturumID 0'dan buyuk olmalidir.");
            }

            if (sample.KullaniciID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(EEGVerisi.KullaniciID), "KullaniciID 0'dan buyuk olmalidir.");
            }

            var record = new EEGVerisi
            {
                OturumID = sample.OturumID,
                KullaniciID = sample.KullaniciID,
                Delta = sample.Delta,
                Theta = sample.Theta,
                LowAlpha = sample.LowAlpha,
                HighAlpha = sample.HighAlpha,
                LowBeta = sample.LowBeta,
                HighBeta = sample.HighBeta,
                LowGamma = sample.LowGamma,
                HighGamma = sample.HighGamma,
                BlinkStrength = sample.BlinkStrength,
                KayitZamani = sample.KayitZamani == default(DateTime) ? DateTime.UtcNow : sample.KayitZamani
            };

            using (var context = DbContextFactory.Create())
            {
                context.EEGVerisi.Add(record);
                await context.SaveChangesAsync();
                return record;
            }
        }
        public async Task DeleteBySessionAsync(int sessionId)
        {
            if (sessionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId));
            }

            using (var context = DbContextFactory.Create())
            {
                var items = await context.EEGVerisi
                    .Where(v => v.OturumID == sessionId)
                    .ToListAsync();

                if (!items.Any())
                {
                    return;
                }

                context.EEGVerisi.RemoveRange(items);
                await context.SaveChangesAsync();
            }
        }
    }
}


