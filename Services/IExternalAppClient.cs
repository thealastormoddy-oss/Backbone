using LabSyncBackbone.Models;

namespace LabSyncBackbone.Services
{
    public interface IExternalAppClient
    {
        Task<ExternalAppResponse> SendAsync(ExternalAppRequest request);
    }
}
