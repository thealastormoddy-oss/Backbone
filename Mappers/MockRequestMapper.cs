using LabSyncBackbone.Models;
using LabSyncBackbone.Services;

namespace LabSyncBackbone.Mappers
{
    public class MockRequestMapper : IRequestMapper
    {
        public ExternalAppRequest Map(SyncRequest request)
        {
            var externalRequest = new ExternalAppRequest();

            externalRequest.SystemName = request.LocalAppName;
            externalRequest.SubmissionId = request.RecordId;

            externalRequest.Customer = new ExternalCustomerInfo();
            externalRequest.Customer.Name = request.Payload?.CustomerName;
            externalRequest.Customer.Code = request.Payload?.CustomerCode;

            externalRequest.Order = new ExternalOrderInfo();
            externalRequest.Order.TotalAmount = request.Payload?.Amount ?? 0;

            externalRequest.Comments = request.Payload?.Notes;

            return externalRequest;
        }
    }
}
