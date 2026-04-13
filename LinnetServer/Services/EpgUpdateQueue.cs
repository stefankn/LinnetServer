using System.Collections.Concurrent;
using System.Threading.Channels;

namespace LinnetServer.Services;

public class EpgUpdateQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    private readonly ConcurrentDictionary<int, bool> _inProgress = new();

    public event Action? OnChanged;

    public void Enqueue(int channelGroupItemId) =>
        _channel.Writer.TryWrite(channelGroupItemId);

    public bool IsInProgress(int channelGroupItemId) =>
        _inProgress.ContainsKey(channelGroupItemId);

    public void MarkStarted(int channelGroupItemId)
    {
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
