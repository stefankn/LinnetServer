using LinnetServer.Data;
using LinnetServer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LinnetServer.Services;

public partial class EpgRefreshWorker(
    EpgUpdateQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EpgRefreshWorker> logger) : BackgroundService
{
    private static readonly TimeOnly RunAt = new(3, 0);
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(6);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await CheckAndEnqueueStaleChannelsAsync(EpgUpdateTrigger.Startup, ct);

        while (!ct.IsCancellationRequested)
        {
            var delay = TimeUntilNext(RunAt);
            LogNextRun(logger, DateTime.Now + delay);
            await Task.Delay(delay, ct);
            await CheckAndEnqueueStaleChannelsAsync(EpgUpdateTrigger.Nightly, ct);
        }
    }

    private static TimeSpan TimeUntilNext(TimeOnly target)
    {
        var now = DateTime.Now;
        var next = now.Date.Add(target.ToTimeSpan());
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }

    public async Task CheckAndEnqueueStaleChannelsAsync(EpgUpdateTrigger trigger = EpgUpdateTrigger.Startup, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - StaleThreshold;

        var staleIds = await db.ChannelGroupItems
            .Where(c => c.EpgFetchFailed || c.EpgLastUpdated == null || c.EpgLastUpdated < cutoff)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (staleIds.Count == 0)
        {
            LogNoStaleChannels(logger);
            return;
        }

        LogEnqueuingStaleChannels(logger, staleIds.Count);

        foreach (var id in staleIds)
            queue.Enqueue(id, trigger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "EPG refresh scheduled, next run at {NextRun:yyyy-MM-dd HH:mm}")]
    private static partial void LogNextRun(ILogger logger, DateTime nextRun);

    [LoggerMessage(Level = LogLevel.Information, Message = "EPG refresh check: all channels are up-to-date")]
    private static partial void LogNoStaleChannels(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "EPG refresh check: enqueueing {Count} stale channel(s)")]
    private static partial void LogEnqueuingStaleChannels(ILogger logger, int count);
}
