using LabSyncBackbone.Models;

namespace LabSyncBackbone.Helpers
{
    public class SyncRequestValidator
    {
        public string? Validate(SyncRequest request)
        {
            if (request == null)
            {
                return "Request body is required.";
            }

            if (string.IsNullOrWhiteSpace(request.LocalAppName))
            {
                return "LocalAppName is required.";
            }

            if (string.IsNullOrWhiteSpace(request.RecordId))
            {
                return "RecordId is required.";
            }

            if (request.Payload == null)
            {
                return "Payload is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Payload.CustomerName))
            {
                return "Payload.CustomerName is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Payload.CustomerCode))
            {
                return "Payload.CustomerCode is required.";
            }

            return null;
        }
    }
}