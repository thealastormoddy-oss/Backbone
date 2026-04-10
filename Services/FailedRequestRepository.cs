using LabSyncBackbone.Data;

namespace LabSyncBackbone.Services
{
    public class FailedRequestRepository : IFailedRequestRepository
    {
        private readonly LabSyncBackboneDbContext _db;

        public FailedRequestRepository(LabSyncBackboneDbContext db)
        {
            _db = db;
        }

        public void Save(FailedRequest failedRequest)
        {
            _db.FailedRequests.Add(failedRequest);
            _db.SaveChanges();
        }

        public FailedRequest? Get(int id)
        {
            return _db.FailedRequests.FirstOrDefault(r => r.Id == id);
        }

        public List<FailedRequest> GetPendingRetries()
        {
            return _db.FailedRequests
                .Where(r => r.Status == FailedRequestStatus.Pending && r.NextRetryAt <= DateTime.UtcNow)
                .ToList();
        }

        public void Update(FailedRequest failedRequest)
        {
            _db.FailedRequests.Update(failedRequest);
            _db.SaveChanges();
        }
    }
}
