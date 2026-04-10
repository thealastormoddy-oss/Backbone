using LabSyncBackbone.Models;

namespace LabSyncBackbone.Services
{
    public interface ICaseStore
    {
        // Checks Redis first, falls back to Postgres.
        // If found in Postgres but not Redis, reloads Redis before returning.
        SyncResponse? Find(string caseId);

        // Saves to BOTH Redis and Postgres in one call.
        void Save(string caseId, string appName, SyncResponse response);

        // Removes from Redis only — Postgres keeps the record.
        // Used for cache invalidation when a webhook says a case changed.
        void Evict(string caseId);

        // Returns the union of all known CaseIds from Redis and Postgres.
        IEnumerable<string> GetAllKeys();
    }
}
