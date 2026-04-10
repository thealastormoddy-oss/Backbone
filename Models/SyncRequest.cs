namespace LabSyncBackbone.Models
{
    public class SyncRequest
    {
        public string? LocalAppName { get; set; }

        public string? RecordId { get; set; }

        public SyncPayload? Payload { get; set; }
    }
}