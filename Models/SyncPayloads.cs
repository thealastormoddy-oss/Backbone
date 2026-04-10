namespace LabSyncBackbone.Models
{
    public class SyncPayload
    {
        public string? CustomerName { get; set; }

        public string? CustomerCode { get; set; }

        public decimal Amount { get; set; }

        public string? Notes { get; set; }
    }
}