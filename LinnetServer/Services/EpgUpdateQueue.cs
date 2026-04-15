using System.Collections.Concurrent;
using System.Threading.Channels;

namespace LinnetServer.Services;

public class EpgUpdateQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    private readonly ConcurrentDictionary<int, bool> _pending = new();
    private readonly ConcurrentDictionary<int, bool> _inProgress = new();

    public event Action? OnChanged;

    public void Enqueue(int channelGroupItemId)
    {
        if (_inProgress.ContainsKey(channelGroupItemId))
            return;
        if (!_pending.TryAdd(channelGroupItemId, true))
            return;
        _channel.Writer.TryWrite(channelGroupItemId);
    }

    public bool IsInProgress(int channelGroupItemId) =>
        _inProgress.ContainsKey(channelGroupItemId);

    public bool IsPending(int channelGroupItemId) =>
        _pending.ContainsKey(channelGroupItemId);

    public void MarkStarted(int channelGroupItemId)
    {
        _pending.TryRemove(channelGroupItemId, out _);
        _inProgress[channelGroupItemId] = true;
        OnChanged?.Invoke();
    }

    public void MarkCompleted(int channelGroupItemId)
    {
        _inProgress.TryRemove(channelGroupItemId, out _);
        OnChanged?.Invoke();
    }

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
