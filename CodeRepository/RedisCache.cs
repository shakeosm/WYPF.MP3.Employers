using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Text;

namespace MCPhase3.CodeRepository
{
    public class RedisCache : IRedisCache
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConfiguration _configuration;
        private readonly int EXPIRY_MINUTES;

        public RedisCache(IDistributedCache distributedCache, IConfiguration configuration)
        {
            _distributedCache = distributedCache;
            _configuration = configuration;
            EXPIRY_MINUTES = _configuration.GetValue<int>("RedisExpiryMinutes");

        }

        public string GetString(string cacheKeyName)
        {
            var cachedValue = _distributedCache.GetString(cacheKeyName);
            return cachedValue ?? "";
        }

        public void SetString(string cacheKeyName, string cacheValue)
        {
            _distributedCache.SetString(cacheKeyName, cacheValue, CacheOption());
        }

        public void Set<T>(string cacheKeyName, T value)
        {
            if (value is not null)
            {
                var jsonData = JsonConvert.SerializeObject(value);
                _distributedCache.SetString(cacheKeyName, jsonData, CacheOption());
            }
        }

        public T Get<T>(string cacheKeyName)
        {
            var jsonData = _distributedCache.GetString(cacheKeyName);

            if (jsonData is null)
            {
                return default;
            }

            var cachedValue = JsonConvert.DeserializeObject<T>(jsonData);
            return cachedValue;
        }


        DistributedCacheEntryOptions CacheOption()
        {
            return new DistributedCacheEntryOptions
            {
                //AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(EXPIRY_MINUTES),
                SlidingExpiration = TimeSpan.FromMinutes(EXPIRY_MINUTES)
            };
        }

        public void Delete(string cacheKeyName)
        {
            _distributedCache.Remove(cacheKeyName);
        }
    }

    public interface IRedisCache
    {
        public string GetString(string cacheKeyName);
        public T Get<T>(string cacheKeyName);
        public void SetString(string cacheKeyName, string cacheValue);
        public void Set<T>(string cacheKeyName, T cacheValue);
        public void Delete(string cacheKeyName);

    }
}
