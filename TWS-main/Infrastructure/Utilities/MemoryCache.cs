using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TWS.Infrastructure.Utilities
{
    /// <summary>
    /// Simple in-memory cache with expiration support
    /// </summary>
    public class MemoryCache<TKey, TValue>
    {
        private class CacheItem
        {
            public TValue Value { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        }

        private readonly ConcurrentDictionary<TKey, CacheItem> _cache;
        private readonly TimeSpan _defaultExpiration;

        /// <summary>
        /// Creates a new memory cache with default expiration time
        /// </summary>
        public MemoryCache(TimeSpan defaultExpiration)
        {
            _cache = new ConcurrentDictionary<TKey, CacheItem>();
            _defaultExpiration = defaultExpiration;
        }

        /// <summary>
        /// Adds or updates a value in the cache
        /// </summary>
        public void Set(TKey key, TValue value)
        {
            Set(key, value, _defaultExpiration);
        }

        /// <summary>
        /// Adds or updates a value in the cache with custom expiration
        /// </summary>
        public void Set(TKey key, TValue value, TimeSpan expiration)
        {
            var item = new CacheItem
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            };

            _cache.AddOrUpdate(key, item, (k, old) => item);
        }

        /// <summary>
        /// Tries to get a value from the cache
        /// </summary>
        public bool TryGet(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (!item.IsExpired)
                {
                    value = item.Value;
                    return true;
                }
                else
                {
                    // Remove expired item
                    _cache.TryRemove(key, out _);
                }
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Gets a value from cache or returns default
        /// </summary>
        public TValue Get(TKey key)
        {
            TryGet(key, out var value);
            return value;
        }

        /// <summary>
        /// Removes a specific key from cache
        /// </summary>
        public bool Remove(TKey key)
        {
            return _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// Clears all items from cache
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Removes all expired items from cache
        /// </summary>
        public void ClearExpired()
        {
            var expiredKeys = new List<TKey>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Checks if a key exists in cache and is not expired
        /// </summary>
        public bool Contains(TKey key)
        {
            return TryGet(key, out _);
        }

        /// <summary>
        /// Gets the number of items in cache (including expired)
        /// </summary>
        public int Count => _cache.Count;
    }
}