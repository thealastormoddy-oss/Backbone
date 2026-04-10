using LabSyncBackbone.AppSettings;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace LabSyncBackbone.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly int _expirationSeconds;

        public RedisCacheService(IConnectionMultiplexer connection, IOptions<CacheSettings> options)
        {
            _connection = connection;
            _expirationSeconds = options.Value.ExpirationSeconds;
        }

        public T? Get<T>(string key)
        {
            var db = _connection.GetDatabase();

            var json = db.StringGet(key);

            if (json.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>((string)json!);
        }

        public void Set<T>(string key, T value)
        {
            var db = _connection.GetDatabase();

            var json = JsonSerializer.Serialize(value);

            db.StringSet(key, json, TimeSpan.FromSeconds(_expirationSeconds));
        }

        public void Remove(string key)
        {
            var db = _connection.GetDatabase();

            db.KeyDelete(key);
        }

        public IEnumerable<string> GetKeys()
        {
            var server = _connection.GetServer(_connection.GetEndPoints().First());

            foreach (var key in server.Keys())
            {
                yield return (string)key!;
            }
        }
    }
}
