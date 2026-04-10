using LabSyncBackbone.AppSettings;
using LabSyncBackbone.Data;
using LabSyncBackbone.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LabSyncBackbone.Services
{
    public class CaseRepository : ICaseRepository
    {
        private readonly LabSyncBackboneDbContext _db;
        private readonly int _expirationSeconds;

        public CaseRepository(LabSyncBackboneDbContext db, IOptions<CacheSettings> options)
        {
            _db = db;
            _expirationSeconds = options.Value.ExpirationSeconds;
        }

        public SyncResponse? Get(string caseId)
        {
            var entry = _db.CachedCases.FirstOrDefault(c => c.CaseId == caseId);

            if (entry == null)
            {
                return null;
            }

            // Lazy expiry: if past the expiry date, delete it and treat as a miss
            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                _db.CachedCases.Remove(entry);
                _db.SaveChanges();
                return null;
            }

            return JsonSerializer.Deserialize<SyncResponse>(entry.Data);
        }

        public void Save(string caseId, string appName, SyncResponse response)
        {
            var json = JsonSerializer.Serialize(response);

            var existing = _db.CachedCases.FirstOrDefault(c => c.CaseId == caseId);

            if (existing != null)
            {
                // Case already exists — update it
                existing.Data = json;
                existing.CachedAt = DateTime.UtcNow;
                existing.ExpiresAt = DateTime.UtcNow.AddSeconds(_expirationSeconds);
            }
            else
            {
                // New case — insert it
                var entry = new CachedCase
                {
                    CaseId = caseId,
                    AppName = appName,
                    Data = json,
                    CachedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(_expirationSeconds)
                };

                _db.CachedCases.Add(entry);
            }

            _db.SaveChanges();
        }

        public IEnumerable<string> GetAllKeys()
        {
            return _db.CachedCases.Select(c => c.CaseId).ToList();
        }

        public void Delete(string caseId)
        {
            var entry = _db.CachedCases.FirstOrDefault(c => c.CaseId == caseId);

            if (entry != null)
            {
                _db.CachedCases.Remove(entry);
                _db.SaveChanges();
            }
        }

        public int DeleteExpired()
        {
            var expiredEntries = _db.CachedCases.Where(c => c.ExpiresAt < DateTime.UtcNow).ToList();

            if (expiredEntries.Count == 0)
            {
                return 0;
            }

            _db.CachedCases.RemoveRange(expiredEntries);
            _db.SaveChanges();

            return expiredEntries.Count;
        }
    }
}
