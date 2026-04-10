using LabSyncBackbone.Models;

namespace LabSyncBackbone.Services
{
    public class CaseStore : ICaseStore
    {
        private readonly ICacheService _cache;
        private readonly ICaseRepository _repository;
        private readonly ILogger<CaseStore> _logger;

        public CaseStore(ICacheService cache, ICaseRepository repository, ILogger<CaseStore> logger)
        {
            _cache = cache;
            _repository = repository;
            _logger = logger;
        }

        public SyncResponse? Find(string caseId)
        {
            // Step 1: Redis
            var cached = _cache.Get<SyncResponse>(caseId);
            if (cached != null)
            {
                _logger.LogInformation("[CaseStore.{Method}] Redis hit for {CaseId}.", nameof(Find), caseId);
                return cached;
            }

            // Step 2: Postgres
            var stored = _repository.Get(caseId);
            if (stored != null)
            {
                _logger.LogInformation("[CaseStore.{Method}] Postgres hit for {CaseId}. Reloading Redis.", nameof(Find), caseId);
                _cache.Set<SyncResponse>(caseId, stored);
                return stored;
            }

            return null;
        }

        public void Save(string caseId, string appName, SyncResponse response)
        {
            _repository.Save(caseId, appName, response);
            _cache.Set<SyncResponse>(caseId, response);
            _logger.LogInformation("[CaseStore.{Method}] Stored in Postgres and Redis for {CaseId}.", nameof(Save), caseId);
        }

        public void Evict(string caseId)
        {
            _cache.Remove(caseId);
            _logger.LogInformation("[CaseStore.{Method}] Evicted {CaseId} from Redis.", nameof(Evict), caseId);
        }

        public IEnumerable<string> GetAllKeys()
        {
            var redisKeys = _cache.GetKeys();
            var postgresKeys = _repository.GetAllKeys();
            return redisKeys.Union(postgresKeys).OrderBy(k => k);
        }
    }
}
