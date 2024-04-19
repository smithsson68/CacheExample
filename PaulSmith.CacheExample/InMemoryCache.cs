using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("PaulSmith.CacheExample.Tests")]

namespace PaulSmith.CacheExample
{
    /// <summary>
    /// General purpose cache.
    /// This class is thread safe so can be used as an application singleton
    /// </summary>
    public class InMemoryCache : ICache
    {
        private readonly ReaderWriterLockSlim _cacheLock = new();
        private readonly LinkedList<object> _lastAccessedList = new();
        private readonly Dictionary<object, CachedItem> _cachedItems;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="maxItemCount">The maximum number of values the cache can hold before
        /// a new insertion causes eviction of the least used value</param>
        public InMemoryCache(int maxItemCount = 100)
        {
            if (maxItemCount <= 0) throw new ArgumentException("Must be a positive value greater than zero", nameof(maxItemCount));

            MaxItemCount = maxItemCount;
            _cachedItems = new Dictionary<object, CachedItem>(maxItemCount);
        }

        /// <summary>
        /// Adds or updates a value in the cache using the <see cref="key"/> specified
        /// If <see cref="MaxItemCount"/> has already been reached then the least recently used
        /// item will be evicted to make space for the new value
        /// </summary>
        /// <typeparam name="TKey">The Type of the <see cref="key"/></typeparam>
        /// <typeparam name="TValue">The Type of the <see cref="value"/></typeparam>
        /// <param name="key">The key to associate with the <see cref="value"/>. Cannot be null</param>
        /// <param name="value">The value to cache. Cannot be null</param>
        /// <param name="evictedFromCacheHandler">An optional handler to be called if the value gets evicted from the cache</param>
        public void AddOrUpdate<TKey, TValue>(
            [DisallowNull]TKey key, 
            [DisallowNull]TValue value, 
            Action<TKey, TValue>? evictedFromCacheHandler = null)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            _cacheLock.EnterWriteLock();

            try
            {
                if (_cachedItems.ContainsKey(key))
                {
                    UpdateExisting(key, value, evictedFromCacheHandler);
                }
                else
                {
                    if (_cachedItems.Count == MaxItemCount)
                    {
                        EvictLeastAccessed();
                    }

                    AddNew(key, value, evictedFromCacheHandler);
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes a value from the cache for the <see cref="key"/> specified
        /// </summary>
        /// <typeparam name="TKey">The Type of the <see cref="key"/></typeparam>
        /// <param name="key">The key associated with the value. Cannot be null</param>
        public bool Remove<TKey>([DisallowNull]TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _cacheLock.EnterWriteLock();

            try
            {
                if (_cachedItems.TryGetValue(key, out var cachedItem))
                {
                    _lastAccessedList.Remove(cachedItem.LastAccessedNode);
                    _cachedItems.Remove(key);
                    return true;
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();       
            }

            return false;
        }

        /// <summary>
        /// Gets the value associated with the <see cref="key"/> specified
        /// </summary>
        /// <typeparam name="TKey">The Type of the <see cref="key"/></typeparam>
        /// <typeparam name="TValue">The Type of the <see cref="value"/></typeparam>
        /// <param name="key">The key associated with the value. Cannot be null</param>
        /// <param name="value">The value associated with the key or default if not found</param>
        /// <returns>True if the value was found and provided, otherwise false</returns>
        public bool TryGet<TKey, TValue>([DisallowNull]TKey key, out TValue? value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            value = default;

            _cacheLock.EnterUpgradeableReadLock();

            try
            {
                if (_cachedItems.ContainsKey(key))
                {
                    _cacheLock.EnterWriteLock();

                    try
                    {
                        // Ensure item still cached
                        if (_cachedItems.TryGetValue(key, out var cachedItem))
                        {
                            SetNodeAccessed(cachedItem.LastAccessedNode);
                            value = (TValue)cachedItem.Value;
                            return true;
                        }
                    }
                    finally
                    {
                        _cacheLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock();
            }

            return false;
        }

        /// <summary>
        /// Checks to see if the cache holds a value for the <see cref="key"/> specified
        /// </summary>
        /// <typeparam name="TKey">The Type of the <see cref="key"/></typeparam>
        /// <param name="key">The key associated with the value. Cannot be null</param>
        /// <returns>True if the value was found in the cache, otherwise false</returns>
        public bool IsCached<TKey>([DisallowNull]TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _cacheLock.EnterReadLock();

            try
            {
                return _cachedItems.ContainsKey(key);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// The maximum number of values that the cache can hold
        /// </summary>
        public int MaxItemCount { get; }

        private void AddNew<TKey, TValue>(
            [DisallowNull] TKey key, 
            [DisallowNull] TValue value,
            Action<TKey, TValue>? evictedFromCacheHandler)
        {
            var lastAccessedNode = _lastAccessedList.AddFirst(key);

            // Update the item
            _cachedItems[key] =
                new CachedItem(
                    value, 
                    lastAccessedNode,
                    MakeEvictedFromCacheHandler(key, value, evictedFromCacheHandler));
        }

        private void EvictLeastAccessed()
        {
            // Remove the least accessed item which be the last node in our list
            var leastAccessedNode = _lastAccessedList.Last;
            var existingCachedItem = _cachedItems[leastAccessedNode!.Value];

            existingCachedItem.EvictedFromCacheHandler?.Invoke();

            _lastAccessedList.RemoveLast();
            _cachedItems.Remove(leastAccessedNode.Value);
        }

        private void UpdateExisting<TKey, TValue>(
            [DisallowNull] TKey key, 
            [DisallowNull] TValue value,
            Action<TKey, TValue>? evictedFromCacheHandler)
        {
            var existingCachedItem = _cachedItems[key];

            SetNodeAccessed(existingCachedItem.LastAccessedNode);

            // Update the item
            _cachedItems[key] =
                new CachedItem(
                    value, 
                    existingCachedItem.LastAccessedNode,
                    MakeEvictedFromCacheHandler(key, value, evictedFromCacheHandler));
        }

        private void SetNodeAccessed(LinkedListNode<object> node)
        {
            // Accessed nodes move to the top of the list
            if (node != _lastAccessedList.First)
            {
                _lastAccessedList.Remove(node);
                _lastAccessedList.AddFirst(node);
            }
        }

        private static Action? MakeEvictedFromCacheHandler<TKey, TValue>(
            TKey key, 
            TValue value, 
            Action<TKey, TValue>? evictedFromCacheHandler)
        {
            if (evictedFromCacheHandler == null)
            {
                return null;
            }

            return () => evictedFromCacheHandler.Invoke(key, value);
        }
    }
}
