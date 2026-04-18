namespace LinnetServer.Services;

public partial class TvLogoRefreshWorker(
    TvLogoService logoService,
    ILogger<TvLogoRefreshWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Fetch immediately on startup if index is missing or stale
        if (!logoService.IsIndexFresh)
        {
            try { await logoService.RefreshIndexAsync(ct); }
            catch (Exception ex) { LogRefreshFailed(logger, ex); }
        }

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromDays(1), ct);

            if (!logoService.IsIndexFresh)
            {
                try { await logoService.RefreshIndexAsync(ct); }
                catch (Exception ex) { LogRefreshFailed(logger, ex); }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to refresh tv-logo index")]
    private static partial void LogRefreshFailed(ILogger logger, Exception ex);
}
