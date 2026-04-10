using LabSyncBackbone.Models;

namespace LabSyncBackbone.Services
{
    public interface IRequestMapper
    {
        ExternalAppRequest Map(SyncRequest request);
    }
}
