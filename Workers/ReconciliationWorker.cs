using LabSyncBackbone.AppSettings;
using LabSyncBackbone.Services;
using Microsoft.Extensions.Options;

namespace LabSyncBackbone.Workers
{
    public class ReconciliationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReconciliationWorker> _logger;
        private readonly WorkerSettings _settings;

        public ReconciliationWorker(IServiceScopeFactory scopeFactory, ILogger<ReconciliationWorker> logger, IOptions<WorkerSettings> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[ReconciliationWorker.{Method}] Started. Interval: {IntervalMinutes} minutes.", nameof(ExecuteAsync), _settings.ReconciliationIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(_settings.ReconciliationIntervalMinutes), stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();

                        var (reloadedIntoRedis, savedToPostgres) = reconciliationService.Reconcile();

                        _logger.LogInformation("[ReconciliationWorker.{Method}] {ReloadedIntoRedis} reloaded into Redis, {SavedToPostgres} saved to Postgres.", nameof(ExecuteAsync), reloadedIntoRedis, savedToPostgres);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ReconciliationWorker.{Method}] Error during reconciliation. Will retry next interval.", nameof(ExecuteAsync));
                }
            }

            _logger.LogInformation("[ReconciliationWorker.{Method}] Stopped.", nameof(ExecuteAsync));
        }
    }
}
