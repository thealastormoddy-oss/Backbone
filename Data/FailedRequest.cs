namespace LabSyncBackbone.Data
{
    public class FailedRequest
    {
        public int Id { get; set; }
        public string CaseId { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string RequestPayload { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public int AttemptCount { get; set; } = 0;
        public DateTime? LastAttemptAt { get; set; }
        public DateTime NextRetryAt { get; set; }
        public FailedRequestStatus Status { get; set; } = FailedRequestStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
