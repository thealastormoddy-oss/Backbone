using LabSyncBackbone.Data;
using LabSyncBackbone.Models;
using System.Text.Json;

namespace LabSyncBackbone.Services
{
    public class SyncService
    {
        private readonly ExternalAppRegistry _registry;
        private readonly ICaseStore _caseStore;
        private readonly IReconciliationService _reconciliationService;
        private readonly IFailedRequestRepository _failedRequestRepository;
        private readonly RetryTrigger _retryTrigger;
        private readonly ILogger<SyncService> _logger;

        public SyncService(ExternalAppRegistry registry, ICaseStore caseStore, IReconciliationService reconciliationService, IFailedRequestRepository failedRequestRepository, RetryTrigger retryTrigger, ILogger<SyncService> logger)
        {
            _registry = registry;
            _caseStore = caseStore;
            _reconciliationService = reconciliationService;
            _failedRequestRepository = failedRequestRepository;
            _retryTrigger = retryTrigger;
            _logger = logger;
        }

        public async virtual Task<SyncResponse> ProcessAsync(SyncRequest request, string appName, string endpoint)
        {
            var caseId = request.RecordId!;

            // Step 1+2: Check local storage (Redis → Postgres fallback)
            var local = _caseStore.Find(caseId);
            if (local != null)
            {
                local.Message = "Response returned from cache.";
                return local;
            }

            // Step 3: Both missed — call the external app
            _logger.LogInformation("[SyncService.{Method}] Local miss for {CaseId}. Calling external app.", nameof(ProcessAsync), caseId);

            var mapper = _registry.GetMapper(appName);
            var externalRequest = mapper.Map(request);
            var client = _registry.GetClient(appName);
            var externalResponse = await client.SendAsync(externalRequest);

            var response = new SyncResponse
            {
                Message = externalResponse.Status == "Success"
                    ? "Request processed successfully."
                    : "Request was received, but external app processing failed.",
                ReceivedFrom = request.LocalAppName,
                RecordId = request.RecordId,
                Payload = request.Payload,
                NextStep = "Internal request was mapped into a different external shape.",
                ExternalStatus = externalResponse.Status,
                ExternalReference = externalResponse.ExternalReference,
                ExternalMessage = externalResponse.Message,
                SentToExternalApp = externalRequest
            };

            if (externalResponse.Status == "Success")
            {
                _caseStore.Save(caseId, appName, response);
                _retryTrigger.Trigger();
            }
            else
            {
                _logger.LogWarning("[SyncService.{Method}] External call failed for {CaseId}. Saving to FailedRequests.", nameof(ProcessAsync), caseId);

                _failedRequestRepository.Save(new FailedRequest
                {
                    CaseId = caseId,
                    AppName = appName,
                    Endpoint = endpoint,
                    RequestPayload = JsonSerializer.Serialize(request),
                    FailureReason = externalResponse.Message ?? "Unknown failure",
                    AttemptCount = 1,
                    LastAttemptAt = DateTime.UtcNow,
                    NextRetryAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, 1) * 60),
                    Status = FailedRequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return response;
        }

        public SyncResponse? GetCase(string caseId)
        {
            return _caseStore.Find(caseId);
        }

        public List<string> GetAllCaseIds()
        {
            var (reloadedIntoRedis, savedToPostgres) = _reconciliationService.Reconcile();

            _logger.LogInformation("[SyncService.{Method}] Reconciliation: {ReloadedIntoRedis} reloaded into Redis, {SavedToPostgres} saved to Postgres.", nameof(GetAllCaseIds), reloadedIntoRedis, savedToPostgres);

            return _caseStore.GetAllKeys().ToList();
        }
    }
}
