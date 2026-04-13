using LinnetServer.Data;
using LinnetServer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LinnetServer.Services;

public class EpgWorker(
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
                        logger.LogWarning("EPG fetch failed for ChannelGroupItem {Id} (attempt {Attempt}/{Max}), retrying in {Delay}s: {Message}",
                            channelGroupItemId, attempt, MaxRetries, delay.TotalSeconds, ex.Message);
                        await Task.Delay(delay, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update EPG for ChannelGroupItem {Id} after {Max} attempts", channelGroupItemId, MaxRetries);
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
            logger.LogWarning("ChannelGroupItem {Id} not found, skipping EPG update", channelGroupItemId);
            return;
        }

        logger.LogInformation("Fetching EPG guide for channel {Name} (stream {StreamId})", item.ChannelName, item.StreamId);

        var listings = await api.GetEpgGuideAsync(item.StreamId);

        // Remove existing programs for this item before reinserting
        await db.ChannelPrograms
            .Where(p => p.ChannelGroupItemId == channelGroupItemId)
            .ExecuteDeleteAsync(ct);

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

        for (var i = 0; i < programs.Count; i += BatchSize)
        {
            db.ChannelPrograms.AddRange(programs.Skip(i).Take(BatchSize));
            await db.SaveChangesAsync(ct);
        }

        item.EpgLastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("EPG update complete for channel {Name}: {Count} programs stored", item.ChannelName, programs.Count);
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
}
