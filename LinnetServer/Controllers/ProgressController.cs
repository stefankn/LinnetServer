using LinnetServer.Data;
using LinnetServer.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinnetServer.Controllers;

public record UpsertWatchProgressRequest(
    WatchProgressContentType ContentType,
    string StreamId,
    string Title,
    int PositionSeconds,
    int DurationSeconds,
    bool IsCompleted,
    string? CoverUrl = null,
    int? SeriesId = null,
    int? SeasonNumber = null,
    int? EpisodeNumber = null
);

public record WatchProgressItem(
    int Id,
    WatchProgressContentType ContentType,
    string StreamId,
    string Title,
    string? CoverUrl,
    int DurationSeconds,
    int? SeriesId,
    int? SeasonNumber,
    int? EpisodeNumber,
    int PositionSeconds,
    bool IsCompleted,
    DateTime UpdatedAt
);

[ApiController]
[Route("api/v1/progress")]
public class ProgressController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertWatchProgressRequest request)
    {
        var existing = await db.WatchProgressItems
            .FirstOrDefaultAsync(w => w.ContentType == request.ContentType && w.StreamId == request.StreamId);

        var now = DateTime.UtcNow;

        if (existing is null)
        {
            existing = new WatchProgress
            {
                ContentType = request.ContentType,
                StreamId = request.StreamId,
                Title = request.Title,
                CoverUrl = request.CoverUrl,
                DurationSeconds = request.DurationSeconds,
                SeriesId = request.SeriesId,
                SeasonNumber = request.SeasonNumber,
                EpisodeNumber = request.EpisodeNumber,
                PositionSeconds = request.PositionSeconds,
                IsCompleted = request.IsCompleted,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.WatchProgressItems.Add(existing);
        }
        else
        {
            existing.Title = request.Title;
            existing.CoverUrl = request.CoverUrl;
            existing.DurationSeconds = request.DurationSeconds;
            existing.SeriesId = request.SeriesId;
            existing.SeasonNumber = request.SeasonNumber;
            existing.EpisodeNumber = request.EpisodeNumber;
            existing.PositionSeconds = request.PositionSeconds;
            existing.IsCompleted = request.IsCompleted;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync();
        return Ok(new WatchProgressItem(
            existing.Id,
            existing.ContentType,
            existing.StreamId,
            existing.Title,
            existing.CoverUrl,
            existing.DurationSeconds,
            existing.SeriesId,
            existing.SeasonNumber,
            existing.EpisodeNumber,
            existing.PositionSeconds,
            existing.IsCompleted,
            existing.UpdatedAt));
    }

    [HttpGet]
    public async Task<IActionResult> GetInProgress()
    {
        var items = await db.WatchProgressItems
            .Where(w => !w.IsCompleted)
            .OrderByDescending(w => w.UpdatedAt)
            .Select(w => new WatchProgressItem(
                w.Id,
                w.ContentType,
                w.StreamId,
                w.Title,
                w.CoverUrl,
                w.DurationSeconds,
                w.SeriesId,
                w.SeasonNumber,
                w.EpisodeNumber,
                w.PositionSeconds,
                w.IsCompleted,
                w.UpdatedAt))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("movie/{streamId}")]
    public async Task<IActionResult> GetMovieProgress(string streamId)
    {
        var item = await db.WatchProgressItems
            .FirstOrDefaultAsync(w => w.ContentType == WatchProgressContentType.Movie && w.StreamId == streamId);

        if (item is null) return NotFound();

        return Ok(new WatchProgressItem(
            item.Id, item.ContentType, item.StreamId, item.Title, item.CoverUrl,
            item.DurationSeconds, item.SeriesId, item.SeasonNumber, item.EpisodeNumber,
            item.PositionSeconds, item.IsCompleted, item.UpdatedAt));
    }

    [HttpGet("series/{seriesId}")]
    public async Task<IActionResult> GetSeriesProgress(int seriesId)
    {
        var items = await db.WatchProgressItems
            .Where(w => w.ContentType == WatchProgressContentType.Episode && w.SeriesId == seriesId)
            .OrderBy(w => w.SeasonNumber)
            .ThenBy(w => w.EpisodeNumber)
            .Select(w => new WatchProgressItem(
                w.Id, w.ContentType, w.StreamId, w.Title, w.CoverUrl,
                w.DurationSeconds, w.SeriesId, w.SeasonNumber, w.EpisodeNumber,
                w.PositionSeconds, w.IsCompleted, w.UpdatedAt))
            .ToListAsync();

        return Ok(items);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.WatchProgressItems.FindAsync(id);
        if (item is null) return NotFound();

        db.WatchProgressItems.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
