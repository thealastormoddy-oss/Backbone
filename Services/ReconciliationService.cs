using LabSyncBackbone.Models;

namespace LabSyncBackbone.Services
{
    public class ReconciliationService : IReconciliationService
    {
        private readonly ICacheService _cacheService;
        private readonly ICaseRepository _caseRepository;
        private readonly ILogger<ReconciliationService> _logger;

        public ReconciliationService(ICacheService cacheService, ICaseRepository caseRepository, ILogger<ReconciliationService> logger)
        {
            _cacheService = cacheService;
            _caseRepository = caseRepository;
            _logger = logger;
        }

        public (int reloadedIntoRedis, int savedToPostgres) Reconcile()
        {
            var redisKeys = _cacheService.GetKeys().ToList();
            var postgresKeys = _caseRepository.GetAllKeys().ToList();

            int reloadedIntoRedis = 0;
            int savedToPostgres = 0;

            // In Postgres but not Redis — reload into Redis
            var inPostgresNotRedis = postgresKeys.Except(redisKeys).ToList();

            foreach (var key in inPostgresNotRedis)
            {
                var response = _caseRepository.Get(key);

                if (response != null)
                {
                    _cacheService.Set<SyncResponse>(key, response);
                    _logger.LogInformation("[ReconciliationService.{Method}] Reloaded {CaseId} from Postgres into Redis.", nameof(Reconcile), key);
                    reloadedIntoRedis++;
                }
            }

            // In Redis but not Postgres — save to Postgres
            var inRedisNotPostgres = redisKeys.Except(postgresKeys).ToList();

            foreach (var key in inRedisNotPostgres)
            {
                var response = _cacheService.Get<SyncResponse>(key);

                if (response != null)
                {
                    _caseRepository.Save(key, "unknown", response);
                    _logger.LogWarning("[ReconciliationService.{Method}] Case {CaseId} was in Redis but missing from Postgres. Saved to Postgres.", nameof(Reconcile), key);
                    savedToPostgres++;
                }
            }

            return (reloadedIntoRedis, savedToPostgres);
        }
    }
}
