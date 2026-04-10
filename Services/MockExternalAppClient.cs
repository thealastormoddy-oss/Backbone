using LabSyncBackbone.AppSettings;
using LabSyncBackbone.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace LabSyncBackbone.Services
{
    public class MockExternalAppClient : IExternalAppClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MockExternalAppClient> _logger;
        private readonly ExternalAppSettings _settings;

        public MockExternalAppClient(HttpClient httpClient, ILogger<MockExternalAppClient> logger, IOptions<ExternalAppsSettings> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = options.Value.Apps["mock"];
        }

        public virtual async Task<ExternalAppResponse> SendAsync(ExternalAppRequest request)
        {
            try
            {
                _logger.LogInformation("[MockExternalAppClient.{Method}] Sending request. SubmissionId: {SubmissionId}", nameof(SendAsync), request.SubmissionId);

                var httpResponse = await _httpClient.PostAsJsonAsync(_settings.ReceivePath, request);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MockExternalAppClient.{Method}] Non-success status code: {StatusCode}. SubmissionId: {SubmissionId}",
                        nameof(SendAsync),
                        (int)httpResponse.StatusCode,
                        request.SubmissionId);

                    return new ExternalAppResponse
                    {
                        Status = "Failed",
                        ExternalReference = null,
                        Message = "External app returned HTTP " + (int)httpResponse.StatusCode
                    };
                }

                var externalResponse = await httpResponse.Content.ReadFromJsonAsync<ExternalAppResponse>();

                if (externalResponse == null)
                {
                    _logger.LogWarning("[MockExternalAppClient.{Method}] Empty response. SubmissionId: {SubmissionId}", nameof(SendAsync), request.SubmissionId);

                    return new ExternalAppResponse
                    {
                        Status = "Failed",
                        ExternalReference = null,
                        Message = "External app returned empty response."
                    };
                }

                _logger.LogInformation(
                    "[MockExternalAppClient.{Method}] Call succeeded. SubmissionId: {SubmissionId}, ExternalReference: {ExternalReference}",
                    nameof(SendAsync),
                    request.SubmissionId,
                    externalResponse.ExternalReference);

                return externalResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[MockExternalAppClient.{Method}] Could not reach external app. SubmissionId: {SubmissionId}", nameof(SendAsync), request.SubmissionId);

                return new ExternalAppResponse
                {
                    Status = "Failed",
                    ExternalReference = null,
                    Message = "Could not reach external app."
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "[MockExternalAppClient.{Method}] Request timed out. SubmissionId: {SubmissionId}", nameof(SendAsync), request.SubmissionId);

                return new ExternalAppResponse
                {
                    Status = "Failed",
                    ExternalReference = null,
                    Message = "External app request timed out."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MockExternalAppClient.{Method}] Unexpected error. SubmissionId: {SubmissionId}", nameof(SendAsync), request.SubmissionId);

                return new ExternalAppResponse
                {
                    Status = "Failed",
                    ExternalReference = null,
                    Message = "Unexpected error while calling external app."
                };
            }
        }
    }
}