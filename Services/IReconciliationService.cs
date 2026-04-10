namespace LabSyncBackbone.Services
{
    public interface IReconciliationService
    {
        (int reloadedIntoRedis, int savedToPostgres) Reconcile();
    }
}
