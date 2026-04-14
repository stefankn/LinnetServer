using LinnetServer.Data;
using LinnetServer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LinnetServer.Services;

public partial class EpgWorker(
    EpgUpdateQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EpgWorker> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)];

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var channelGroupItemId in queue.ReadAllAsync(ct))
        {
            queue.MarkStarted(channelGroupItemId);
            try
            {
                for (var attempt = 1; attempt <= MaxRetries; attempt++)
                {
                    try
                    {
                        await UpdateEpgAsync(channelGroupItemId, ct);
                        break;
                    }
                    catch (HttpRequestException ex) when (attempt < MaxRetries)
                    {
                        var delay = RetryDelays[attempt - 1];
                        LogEpgFetchRetry(logger, channelGroupItemId, attempt, MaxRetries, (int)delay.TotalSeconds, ex.Message);
                        await Task.Delay(delay, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                LogEpgUpdateFailed(logger, ex, channelGroupItemId, MaxRetries);
            }
            finally
            {
                queue.MarkCompleted(channelGroupItemId);
            }
        }
    }

    private async Task UpdateEpgAsync(int channelGroupItemId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var api = scope.ServiceProvider.GetRequiredService<ApiClient>();

        var item = await db.ChannelGroupItems.FindAsync([channelGroupItemId], ct);
        if (item is null)
        {
            LogChannelGroupItemNotFound(logger, channelGroupItemId);
            return;
        }

        LogFetchingEpg(logger, item.ChannelName, item.StreamId);

        var listings = await api.GetEpgGuideAsync(item.StreamId);

        var programs = listings
            .Where(l => long.TryParse(l.StartTimestamp, out _) && long.TryParse(l.EndTimestamp, out _))
            .Select(l => new ChannelProgram
            {
                ChannelGroupItemId = channelGroupItemId,
                EpgId = l.EpgId,
                Title = DecodeBase64(l.Title),
                Description = DecodeBase64(l.Description),
                ChannelId = l.ChannelId,
                StartTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(l.StartTimestamp)).UtcDateTime,
                EndTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(l.EndTimestamp)).UtcDateTime,
            }).ToList();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.ChannelPrograms
            .Where(p => p.ChannelGroupItemId == channelGroupItemId)
            .ExecuteDeleteAsync(ct);

        for (var i = 0; i < programs.Count; i += BatchSize)
        {
            db.ChannelPrograms.AddRange(programs.Skip(i).Take(BatchSize));
            await db.SaveChangesAsync(ct);
        }

        item.EpgLastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        LogEpgUpdateComplete(logger, item.ChannelName, programs.Count);
    }

    private static string DecodeBase64(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        try
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch
        {
            return value;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "ChannelGroupItem {Id} not found, skipping EPG update")]
    private static partial void LogChannelGroupItemNotFound(ILogger logger, int id);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching EPG guide for channel {Name} (stream {StreamId})")]
    private static partial void LogFetchingEpg(ILogger logger, string name, int streamId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "EPG fetch failed for ChannelGroupItem {Id} (attempt {Attempt}/{Max}), retrying in {Delay}s: {Message}")]
    private static partial void LogEpgFetchRetry(ILogger logger, int id, int attempt, int max, int delay, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update EPG for ChannelGroupItem {Id} after {Max} attempts")]
    private static partial void LogEpgUpdateFailed(ILogger logger, Exception ex, int id, int max);

    [LoggerMessage(Level = LogLevel.Information, Message = "EPG update complete for channel {Name}: {Count} programs stored")]
    private static partial void LogEpgUpdateComplete(ILogger logger, string name, int count);
}
