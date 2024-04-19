using System.Diagnostics.CodeAnalysis;

namespace PaulSmith.CacheExample
{
    public interface ICache
    {
        /// <summary>
        /// Adds or updates a value in the cache using the <see cref="key"/> specified
        /// If <see cref="InMemoryCache.MaxItemCount"/> has already been reached then the least recently used
        /// item will be evicted to make space for the new value
        /// </summary>
        /// <typeparam name="TKey">The Type of the <see cref="key"/></typeparam>
        /// <typeparam name="TValue">The Type of the <see cref="value"/></typeparam>
        /// <param name="key">The key to associate with the <see cref="value"/>. Cannot be null</param>
        /// <param name="value">The value to cache. Cannot be null</param>
        /// <param name="evictedFromCacheHandler">An optional handler to be called if the value gets evicted from the cache</param>
        void AddOrUpdate<TKey, TValue>(
            [DisallowNull] TKey key,
            [DisallowNull] TValue value,
            Action<TKey, TValue>? evictedFromCacheHandler = null);

        /// <summary>
        /// Removes a value from the cache for the <see cref="key"/> specified
        /// </summary>
        /// <typeparam name="TKey">The Type of the <see cref="key"/></typeparam>
        /// <param name="key">The key associated with the value. Cannot be null</param>
        bool Remove<TKey>([DisallowNull] TKey key);

        /// <summary>
        /// Gets the value associated with the <see cref="key"/> specified
        /// </summary>
        /// <typeparam name="TKey">The Type of the <see cref="key"/></typeparam>
        /// <typeparam name="TValue">The Type of the <see cref="value"/></typeparam>
        /// <param name="key">The key associated with the value. Cannot be null</param>
        /// <param name="value">The value associated with the key or default if not found</param>
        /// <returns>True if the value was found and provided, otherwise false</returns>
        bool TryGet<TKey, TValue>([DisallowNull] TKey key, out TValue? value);

        /// <summary>
        /// Checks to see if the cache holds a value for the <see cref="key"/> specified
        /// </summary>
        /// <typeparam name="TKey">The Type of the <see cref="key"/></typeparam>
        /// <param name="key">The key associated with the value. Cannot be null</param>
        /// <returns>True if the value was found in the cache, otherwise false</returns>
        bool IsCached<TKey>([DisallowNull] TKey key);

        /// <summary>
        /// The maximum number of values that the cache can hold
        /// </summary>
        int MaxItemCount { get; }
    }
}
