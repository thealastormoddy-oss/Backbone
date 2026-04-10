namespace LabSyncBackbone.Models
{
    public class ExternalAppRequest
    {
        public string? SystemName { get; set; }

        public string? SubmissionId { get; set; }

        public ExternalCustomerInfo? Customer { get; set; }

        public ExternalOrderInfo? Order { get; set; }

        public string? Comments { get; set; }
    }

    public class ExternalCustomerInfo
    {
        public string? Name { get; set; }

        public string? Code { get; set; }
    }

    public class ExternalOrderInfo
    {
        public decimal TotalAmount { get; set; }
    }
}