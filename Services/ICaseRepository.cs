using LabSyncBackbone.Models;

namespace LabSyncBackbone.Services
{
    public interface ICaseRepository
    {
        SyncResponse? Get(string caseId);

        void Save(string caseId, string appName, SyncResponse response);

        IEnumerable<string> GetAllKeys();

        void Delete(string caseId);

        int DeleteExpired();
    }
}
