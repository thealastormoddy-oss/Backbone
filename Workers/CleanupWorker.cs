using LabSyncBackbone.AppSettings;
using LabSyncBackbone.Services;
using Microsoft.Extensions.Options;

namespace LabSyncBackbone.Workers
{
    public class CleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CleanupWorker> _logger;
        private readonly WorkerSettings _settings;

        public CleanupWorker(IServiceScopeFactory scopeFactory, ILogger<CleanupWorker> logger, IOptions<WorkerSettings> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[CleanupWorker.{Method}] Started. Interval: {IntervalMinutes} minutes.", nameof(ExecuteAsync), _settings.CleanupIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(_settings.CleanupIntervalMinutes), stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var caseRepository = scope.ServiceProvider.GetRequiredService<ICaseRepository>();

                        var deletedCount = caseRepository.DeleteExpired();

                        _logger.LogInformation("[CleanupWorker.{Method}] Deleted {Count} expired case(s) from Postgres.", nameof(ExecuteAsync), deletedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CleanupWorker.{Method}] Error during cleanup. Will retry next interval.", nameof(ExecuteAsync));
                }
            }

            _logger.LogInformation("[CleanupWorker.{Method}] Stopped.", nameof(ExecuteAsync));
        }
    }
}
