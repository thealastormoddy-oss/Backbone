namespace LabSyncBackbone.AppSettings
{
    public class WorkerSettings
    {
        public int CleanupIntervalMinutes { get; set; }
        public int ReconciliationIntervalMinutes { get; set; }
        public int RetryIntervalMinutes { get; set; }
        public int RetryMaxAttempts { get; set; }
        public int RetryBaseBackoffSeconds { get; set; }
    }
}
