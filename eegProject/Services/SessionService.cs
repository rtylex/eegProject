using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace eegProject.Services
{
    internal sealed class SessionService
    {
        private readonly DeneyTuruService _deneyTuruService = new DeneyTuruService();
        private readonly ZamanEtiketiService _zamanEtiketiService = new ZamanEtiketiService();

        public async Task<List<Oturum>> GetAllAsync()
        {
            using (var context = DbContextFactory.Create())
            {
                return await context.Oturum
                    .Include(o => o.Kullanici)
                    .OrderByDescending(o => o.KayitBaslangic)
                    .ToListAsync();
            }
        }

        public async Task<List<Oturum>> GetByUserAsync(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var context = DbContextFactory.Create())
            {
                return await context.Oturum
                    .Include(o => o.Kullanici)
                    .Where(o => o.KullaniciID == userId)
                    .OrderByDescending(o => o.KayitBaslangic)
                    .ToListAsync();
            }
        }

        public async Task<Oturum> CreateAsync(Oturum session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.KullaniciID <= 0)
            {
                throw new ArgumentException("KullaniciID is required", nameof(session));
            }

            // DeneyTuru string'inden ID bul/oluştur
            if (!string.IsNullOrWhiteSpace(session.DeneyTuru))
            {
                var deneyTuru = await _deneyTuruService.GetOrCreateAsync(session.DeneyTuru);
                session.DeneyTuruID = deneyTuru?.DeneyTuruID;
            }

            // ZamanEtiketi string'inden ID bul/oluştur
            if (!string.IsNullOrWhiteSpace(session.ZamanEtiketi))
            {
                var zamanEtiketi = await _zamanEtiketiService.GetOrCreateAsync(session.ZamanEtiketi);
                session.ZamanEtiketiID = zamanEtiketi?.ZamanEtiketiID;
            }

            using (var context = DbContextFactory.Create())
            {
                session.KayitBaslangic = session.KayitBaslangic == default ? DateTime.UtcNow : session.KayitBaslangic;
                context.Oturum.Add(session);
                await context.SaveChangesAsync();
                return session;
            }
        }

        public async Task UpdateAsync(Oturum session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            // DeneyTuru string'inden ID bul/oluştur
            if (!string.IsNullOrWhiteSpace(session.DeneyTuru))
            {
                var deneyTuru = await _deneyTuruService.GetOrCreateAsync(session.DeneyTuru);
                session.DeneyTuruID = deneyTuru?.DeneyTuruID;
            }
            else
            {
                session.DeneyTuruID = null;
            }

            // ZamanEtiketi string'inden ID bul/oluştur
            if (!string.IsNullOrWhiteSpace(session.ZamanEtiketi))
            {
                var zamanEtiketi = await _zamanEtiketiService.GetOrCreateAsync(session.ZamanEtiketi);
                session.ZamanEtiketiID = zamanEtiketi?.ZamanEtiketiID;
            }
            else
            {
                session.ZamanEtiketiID = null;
            }

            using (var context = DbContextFactory.Create())
            {
                var existing = await context.Oturum.FirstOrDefaultAsync(o => o.OturumID == session.OturumID);
                if (existing == null)
                {
                    throw new InvalidOperationException("Session not found");
                }

                existing.KullaniciID = session.KullaniciID;
                existing.ZamanEtiketi = session.ZamanEtiketi;
                existing.DeneyTuru = session.DeneyTuru;
                existing.DeneyTuruID = session.DeneyTuruID;
                existing.ZamanEtiketiID = session.ZamanEtiketiID;
                existing.KayitBaslangic = session.KayitBaslangic;
                existing.KayitBitis = session.KayitBitis;
                existing.Notlar = session.Notlar;

                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int sessionId)
        {
            if (sessionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId));
            }

            using (var context = DbContextFactory.Create())
            {
                var session = await context.Oturum
                    .Include(o => o.AnalizSonucu)
                    .Include(o => o.EEGVerisi)
                    .Include(o => o.SinavAtama)
                    .Include(o => o.SinavSonucu)
                    .Include(o => o.SinavSonucu.Select(ss => ss.SinavCevap))
                    .FirstOrDefaultAsync(o => o.OturumID == sessionId);

                if (session == null)
                {
                    return;
                }

                // 1. Unlink SinavAtama (Assignments)
                // Oturum silindiğinde atama boşa çıkar, tekrar atanabilir/tamamlanabilir hale gelir.
                foreach (var atama in session.SinavAtama.ToList())
                {
                    atama.OturumID = null;
                    atama.TamamlandiMi = false;
                    atama.TamamlanmaTarihi = null;
                }

                // 2. Delete SinavSonucu (Exam Results) and their Answers
                foreach (var sonuc in session.SinavSonucu.ToList())
                {
                    if (sonuc.SinavCevap != null)
                    {
                        context.SinavCevap.RemoveRange(sonuc.SinavCevap);
                    }
                    context.SinavSonucu.Remove(sonuc);
                }

                // 3. Delete AnalizSonucu (Analysis Results)
                if (session.AnalizSonucu != null)
                {
                    context.AnalizSonucu.RemoveRange(session.AnalizSonucu);
                }

                // 4. Delete EEGVerisi (EEG Data)
                if (session.EEGVerisi != null)
                {
                    context.EEGVerisi.RemoveRange(session.EEGVerisi);
                }

                // 5. Delete Session
                context.Oturum.Remove(session);
                await context.SaveChangesAsync();
            }
        }

        public async Task UpdateRecordEndAsync(int sessionId, DateTime endTime)
        {
            if (sessionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId));
            }

            using (var context = DbContextFactory.Create())
            {
                var session = await context.Oturum.FirstOrDefaultAsync(o => o.OturumID == sessionId);
                if (session != null)
                {
                    session.KayitBitis = endTime;
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
