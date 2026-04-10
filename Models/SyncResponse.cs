namespace LabSyncBackbone.Models
{
    public class SyncResponse
    {
        public string? Message { get; set; }

        public string? ReceivedFrom { get; set; }

        public string? RecordId { get; set; }

        public SyncPayload? Payload { get; set; }

        public string? NextStep { get; set; }

        public string? ExternalStatus { get; set; }

        public string? ExternalReference { get; set; }

        public string? ExternalMessage { get; set; }

        public ExternalAppRequest? SentToExternalApp { get; set; }
    }
}