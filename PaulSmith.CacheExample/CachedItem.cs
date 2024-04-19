namespace PaulSmith.CacheExample;

internal class CachedItem
{
    internal CachedItem(
        object value, 
        LinkedListNode<object> lastAccessedNode,
        Action? evictedFromCacheHandler)
    {
        Value = value;
        LastAccessedNode = lastAccessedNode;
        EvictedFromCacheHandler = evictedFromCacheHandler;
    }

    internal object Value { get; }
    internal LinkedListNode<object> LastAccessedNode { get; }
    internal Action? EvictedFromCacheHandler { get; }
}