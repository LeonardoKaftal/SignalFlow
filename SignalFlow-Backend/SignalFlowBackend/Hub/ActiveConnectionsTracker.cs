namespace SignalFlowBackend.Hub;

public sealed class ActiveConnectionsTracker
{
    private readonly Dictionary<Guid, HashSet<string>> _groups = new();
    
    private readonly Lock _lock = new();

    public void Add(Guid conversationId, string connectionId)
    {
        lock (_lock)
        {
            if (!_groups.TryGetValue(conversationId, out var set))
                _groups[conversationId] = set = [];
            set.Add(connectionId);
        }
    }

    public void Remove(Guid conversationId, string connectionId)
    {
        lock (_lock)
        {
            if (_groups.TryGetValue(conversationId, out var set))
            {
                set.Remove(connectionId);
                if (set.Count == 0) _groups.Remove(conversationId);
            }
        }
    }

    public int Count(Guid conversationId)
    {
        lock (_lock)
        {
            return _groups.TryGetValue(conversationId, out var set) ? set.Count : 0;
        }
    }
}