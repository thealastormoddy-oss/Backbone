namespace LabSyncBackbone.Services
{
    public class ExternalAppRegistry
    {
        private readonly Dictionary<string, IExternalAppClient> _clients = new();
        private readonly Dictionary<string, IRequestMapper> _mappers = new();

        public void Register(string appName, IExternalAppClient client)
        {
            _clients[appName] = client;
        }

        public IExternalAppClient GetClient(string appName)
        {
            if (!_clients.ContainsKey(appName))
            {
                throw new InvalidOperationException("No client registered for app: " + appName);
            }

            return _clients[appName];
        }

        public void RegisterMapper(string appName, IRequestMapper mapper)
        {
            _mappers[appName] = mapper;
        }

        public IRequestMapper GetMapper(string appName)
        {
            if (!_mappers.ContainsKey(appName))
            {
                throw new InvalidOperationException("No mapper registered for app: " + appName);
            }

            return _mappers[appName];
        }
    }
}
