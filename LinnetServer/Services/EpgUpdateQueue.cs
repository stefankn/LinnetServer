using System.Collections.Concurrent;
using System.Threading.Channels;
using LinnetServer.Data.Models;

namespace LinnetServer.Services;

public record EpgQueueItem(int ChannelGroupItemId, EpgUpdateTrigger Trigger);

public class EpgUpdateQueue
{
    private readonly Channel<EpgQueueItem> _channel = Channel.CreateUnbounded<EpgQueueItem>();
    private readonly ConcurrentDictionary<int, EpgUpdateTrigger> _pending = new();
    private readonly ConcurrentDictionary<int, bool> _inProgress = new();

    public event Action? OnChanged;

    public void Enqueue(int channelGroupItemId, EpgUpdateTrigger trigger)
    {
        if (_inProgress.ContainsKey(channelGroupItemId))
            return;
        if (!_pending.TryAdd(channelGroupItemId, trigger))
            return;
        _channel.Writer.TryWrite(new EpgQueueItem(channelGroupItemId, trigger));
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

    public IAsyncEnumerable<EpgQueueItem> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
