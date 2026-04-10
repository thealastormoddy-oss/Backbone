using LabSyncBackbone.AppSettings;
using LabSyncBackbone.Data;
using LabSyncBackbone.Models;
using LabSyncBackbone.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LabSyncBackbone.Workers
{
    public class RetryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RetryWorker> _logger;
        private readonly WorkerSettings _settings;
        private readonly RetryTrigger _retryTrigger;

        public RetryWorker(IServiceScopeFactory scopeFactory, ILogger<RetryWorker> logger, IOptions<WorkerSettings> options, RetryTrigger retryTrigger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = options.Value;
            _retryTrigger = retryTrigger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[RetryWorker.{Method}] Started. Interval: {IntervalMinutes} minutes.", nameof(ExecuteAsync), _settings.RetryIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                var triggered = await _retryTrigger.WaitAsync(
                    TimeSpan.FromMinutes(_settings.RetryIntervalMinutes),
                    stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (triggered)
                    _logger.LogInformation("[RetryWorker.{Method}] Woken by success trigger.", nameof(ExecuteAsync));
                else
                    _logger.LogInformation("[RetryWorker.{Method}] Woken by timer.", nameof(ExecuteAsync));

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var failedRequestRepository = scope.ServiceProvider.GetRequiredService<IFailedRequestRepository>();
                        var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();

                        var pendingRetries = failedRequestRepository.GetPendingRetries();

                        _logger.LogInformation("[RetryWorker.{Method}] {Count} pending retries found.", nameof(ExecuteAsync), pendingRetries.Count);

                        foreach (var failedRequest in pendingRetries)
                        {
                            try
                            {
                                var request = JsonSerializer.Deserialize<SyncRequest>(failedRequest.RequestPayload);

                                if (request == null)
                                {
                                    _logger.LogError("[RetryWorker.{Method}] Could not deserialize payload for FailedRequest {Id}. Marking as Exhausted.", nameof(ExecuteAsync), failedRequest.Id);
                                    failedRequest.Status = FailedRequestStatus.Exhausted;
                                    failedRequestRepository.Update(failedRequest);
                                    continue;
                                }

                                var response = await syncService.ProcessAsync(request, failedRequest.AppName, failedRequest.Endpoint);

                                if (response.ExternalStatus == "Success")
                                {
                                    _logger.LogInformation("[RetryWorker.{Method}] Retry succeeded for {CaseId}. Removing from FailedRequests.", nameof(ExecuteAsync), failedRequest.CaseId);
                                    failedRequest.Status = FailedRequestStatus.Succeeded;
                                    failedRequestRepository.Update(failedRequest);
                                }
                                else
                                {
                                    failedRequest.AttemptCount++;
                                    failedRequest.LastAttemptAt = DateTime.UtcNow;
                                    failedRequest.FailureReason = response.ExternalMessage ?? "Unknown failure";

                                    if (failedRequest.AttemptCount >= _settings.RetryMaxAttempts)
                                    {
                                        failedRequest.Status = FailedRequestStatus.Exhausted;
                                        _logger.LogError("[RetryWorker.{Method}] Case {CaseId} exhausted after {Attempts} attempts.", nameof(ExecuteAsync), failedRequest.CaseId, failedRequest.AttemptCount);
                                    }
                                    else
                                    {
                                        var backoffSeconds = Math.Pow(2, failedRequest.AttemptCount) * _settings.RetryBaseBackoffSeconds;
                                        failedRequest.NextRetryAt = DateTime.UtcNow.AddSeconds(backoffSeconds);
                                        _logger.LogWarning("[RetryWorker.{Method}] Retry failed for {CaseId}. Attempt {Attempts}. Next retry at {NextRetryAt}.", nameof(ExecuteAsync), failedRequest.CaseId, failedRequest.AttemptCount, failedRequest.NextRetryAt);
                                    }

                                    failedRequestRepository.Update(failedRequest);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[RetryWorker.{Method}] Unexpected error retrying FailedRequest {Id}.", nameof(ExecuteAsync), failedRequest.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[RetryWorker.{Method}] Error during retry run. Will retry next interval.", nameof(ExecuteAsync));
                }
            }

            _logger.LogInformation("[RetryWorker.{Method}] Stopped.", nameof(ExecuteAsync));
        }
    }
}
