using LabSyncBackbone.Data;

namespace LabSyncBackbone.Services
{
    public interface IFailedRequestRepository
    {
        void Save(FailedRequest failedRequest);
        FailedRequest? Get(int id);
        List<FailedRequest> GetPendingRetries();
        void Update(FailedRequest failedRequest);
    }
}
