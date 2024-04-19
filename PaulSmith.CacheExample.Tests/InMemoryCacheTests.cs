using Shouldly;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace PaulSmith.CacheExample.Tests
{
    public class InMemoryCacheTests
    {
        [Fact]
        public void Should_Add()
        {
            // Arrange
            var key = "My Key";
            var sut = new InMemoryCache();

            // Act
            sut.AddOrUpdate(key, 23);

            // Assert
            var cachedItems = GetCachedItemsField(sut);
            
            cachedItems.Count.ShouldBe(1);
            cachedItems.ContainsKey(key).ShouldBeTrue();

            var cachedItem = cachedItems[key];

            cachedItem.Value.ShouldBe(23);
            cachedItem.EvictedFromCacheHandler.ShouldBeNull();

            var lastAccessedItems = GetLastAccessedListField(sut);
            lastAccessedItems.Count.ShouldBe(1);
            lastAccessedItems.First!.ShouldBe(cachedItem.LastAccessedNode);
            lastAccessedItems.First!.Value.ShouldBe(key);
        }

        [Fact]
        public void Should_Update()
        {
            // Arrange
            var key = "My Key";
            var sut = new InMemoryCache();

            var lastAccessedItems = GetLastAccessedListField(sut);
            var cachedItems = GetCachedItemsField(sut);

            var lastAccessedItem1 = AddLastAccessedItem(lastAccessedItems, key);
            AddCachedItem(cachedItems, key, 23, lastAccessedItem1, null);

            var lastAccessedItem2 = AddLastAccessedItem(lastAccessedItems, "Another Key");
            var cacheItem2 = AddCachedItem(cachedItems, "Another Key", 24, lastAccessedItem2, null);

            // Act
            sut.AddOrUpdate(key, "Something else");

            // Assert
            cachedItems.Count.ShouldBe(2);
            cachedItems.ContainsKey(key).ShouldBeTrue();

            var updatedCachedItem = cachedItems[key];

            updatedCachedItem.Value.ShouldBe("Something else");
            updatedCachedItem.EvictedFromCacheHandler.ShouldBeNull();
      
            lastAccessedItems.Count.ShouldBe(2);
            lastAccessedItems.First!.ShouldBe(updatedCachedItem.LastAccessedNode);
            lastAccessedItems.First!.Next.ShouldBe(cacheItem2.LastAccessedNode);
        }

        [Fact]
        public void Should_Remove()
        {
            // Arrange
            var key = "My Key";
            var sut = new InMemoryCache();

            var lastAccessedItems = GetLastAccessedListField(sut);
            var lastAccessedItem = AddLastAccessedItem(lastAccessedItems, key);

            var cachedItems = GetCachedItemsField(sut);
            AddCachedItem(cachedItems, key, 23, lastAccessedItem, null);

            // Act
            sut.Remove(key).ShouldBeTrue();
            
            // Assert
            cachedItems.Count.ShouldBe(0);
            lastAccessedItems.Count.ShouldBe(0);
        }

        [Fact]
        public void Should_Get()
        {
            // Arrange
            var key = "My Key";
            var sut = new InMemoryCache();

            var lastAccessedItems = GetLastAccessedListField(sut);
            var lastAccessedItem = AddLastAccessedItem(lastAccessedItems, key);

            var cachedItems = GetCachedItemsField(sut);
            AddCachedItem(cachedItems, key, 23, lastAccessedItem, null);

            // Act
            sut.TryGet(key, out int value).ShouldBeTrue();

            // Assert
            value.ShouldBe(23);
        }

        [Fact]
        public void Should_Not_Get_When_ValueNotCached()
        {
            // Arrange
            var sut = new InMemoryCache();

            // Act
            sut.TryGet("My Key", out int value).ShouldBeFalse();

            // Assert
            value.ShouldBe(0);
        }

        [Fact]
        public void Should_Be_InCache()
        {
            // Arrange
            var key = "My Key";
            var sut = new InMemoryCache();

            var lastAccessedItems = GetLastAccessedListField(sut);
            var lastAccessedItem = AddLastAccessedItem(lastAccessedItems, key);

            var cachedItems = GetCachedItemsField(sut);
            AddCachedItem(cachedItems, key, 23, lastAccessedItem, null);

            // Act and Assert
            sut.IsCached(key).ShouldBeTrue();
        }

        [Fact]
        public void Should_Not_Be_InCache()
        {
            // Arrange
            var sut = new InMemoryCache();

            // Act and Assert
            sut.IsCached("My Key").ShouldBeFalse();
        }

        [Fact]
        public void Should_Evict_When_Full()
        {
            // Arrange
            var sut = new InMemoryCache(2);

            var lastAccessedItems = GetLastAccessedListField(sut);
            var cachedItems = GetCachedItemsField(sut);

            var lastAccessedItem1 = AddLastAccessedItem(lastAccessedItems, "Key 1");
            AddCachedItem(cachedItems, "Key 1", 23, lastAccessedItem1, null);

            var lastAccessedItem2 = AddLastAccessedItem(lastAccessedItems, "Key 2");
            var cachedItem2 = AddCachedItem(cachedItems, "Key 2", 24, lastAccessedItem2, null);

            // Act
            sut.AddOrUpdate("Key 3", 25);

            // Assert
            cachedItems.Count.ShouldBe(2);
            cachedItems.ShouldContainKey("Key 2");
            cachedItems.ShouldContainKey("Key 3");

            var newCachedItem = cachedItems["Key 3"];

            newCachedItem.Value.ShouldBe(25);
            newCachedItem.EvictedFromCacheHandler.ShouldBeNull();
       
            lastAccessedItems.Count.ShouldBe(2);
            lastAccessedItems.First!.ShouldBe(newCachedItem.LastAccessedNode);
            lastAccessedItems.First!.Value.ShouldBe("Key 3");
            lastAccessedItems.First!.Next.ShouldBe(cachedItem2.LastAccessedNode);
        }

        [Fact]
        public void Should_Evict_When_Full_And_CallEvictionHandler()
        {
            // Arrange
            var sut = new InMemoryCache(2);

            var evictionHandlerCalledForCorrectItem = false;

            var evictionHandler = (string k, int v) =>
            {
                if (k == "Key 1" && v == 23)
                {
                    evictionHandlerCalledForCorrectItem = true;
                }
            };

            var lastAccessedItems = GetLastAccessedListField(sut);
            var cachedItems = GetCachedItemsField(sut);

            var lastAccessedItem1 = AddLastAccessedItem(lastAccessedItems, "Key 1");
            AddCachedItem(cachedItems, "Key 1", 23, lastAccessedItem1, evictionHandler);

            var lastAccessedItem2 = AddLastAccessedItem(lastAccessedItems, "Key 2");
            AddCachedItem(cachedItems, "Key 2", 24, lastAccessedItem2, evictionHandler);

            // Act
            sut.AddOrUpdate("Key 3", 25);

            // Assert
            evictionHandlerCalledForCorrectItem.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_Concurrent()
        {
            // Arrange
            var sut = new InMemoryCache(90);

            // Act
            Parallel.For(0, 100, i =>
            {
                sut.AddOrUpdate(i, $"Value {i}");
            });

            // Assert
            var lastAccessedItems = GetLastAccessedListField(sut);
            var cachedItems = GetCachedItemsField(sut);

            lastAccessedItems.Count.ShouldBe(90);
            cachedItems.Count.ShouldBe(90);

            foreach (var lastAccessedItem in lastAccessedItems)
            {
                cachedItems.TryGetValue(lastAccessedItem, out var cachedItem).ShouldBeTrue();
                cachedItem!.Value.ShouldBe($"Value {(int)lastAccessedItem}");
                cachedItem.EvictedFromCacheHandler.ShouldBe(null);
                cachedItem.LastAccessedNode.Value.ShouldBe(lastAccessedItem);
            }
        }

        private Dictionary<object, CachedItem> GetCachedItemsField(InMemoryCache cache)
        {
            return cache.GetPrivate<Dictionary<object, CachedItem>>("_cachedItems");
        }

        private LinkedList<object> GetLastAccessedListField(InMemoryCache cache)
        {
            return cache.GetPrivate<LinkedList<object>>("_lastAccessedList");
        }

        private CachedItem AddCachedItem<TKey, TValue>(
            Dictionary<object, CachedItem> items,
            [DisallowNull]TKey key,
            [DisallowNull]TValue value,
            [DisallowNull]LinkedListNode<object> lastAccessedItem,
            Action<TKey, TValue>? evictedFromCacheHandler)
        {
            var newItem = new CachedItem(
                value, 
                lastAccessedItem,
                evictedFromCacheHandler != null
                    ? () => evictedFromCacheHandler.Invoke(key, value)
                    : null);

            items.Add(key, newItem);
            return newItem;
        }

        private LinkedListNode<object> AddLastAccessedItem(LinkedList<object> items, object key)
        {
            var newItem = new LinkedListNode<object>(key);
            items.AddFirst(newItem);
            return newItem;
        }
    }
}
