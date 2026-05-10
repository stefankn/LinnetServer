using LinnetServer.Data;
using LinnetServer.Data.Models;
using LinnetServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinnetServer.Controllers;

[ApiController]
[Route("api/v1/groups")]
public class GroupsController(AppDbContext db, IOptions<ApiClientOptions> apiOptions, ApiClient api) : ControllerBase
{
    /// <remarks>Returns all channel groups.</remarks>
    [HttpGet]
    public async Task<IActionResult> GetGroups() =>
        Ok(await db.ChannelGroups
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name, g.Type })
            .ToListAsync());

    /// <remarks>Returns all channels belonging to a group.</remarks>
    [HttpGet("{id}/channels")]
    public async Task<IActionResult> GetChannels(int id)
    {
        if (!await db.ChannelGroups.AnyAsync(g => g.Id == id))
            return NotFound();

        var opts = apiOptions.Value;
        var now = DateTime.UtcNow;
        var channels = await db.ChannelGroupItems
            .Where(c => c.ChannelGroupId == id)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.ChannelName,
                c.StreamId,
                c.StreamIcon,
                c.CustomLogoPath,
                c.EpgChannelId,
                c.SortOrder,
                c.IsFavorite,
                CurrentProgram = c.Programs
                    .Where(p => p.StartTime <= now && p.EndTime >= now)
                    .Select(p => new { p.Id, p.Title, p.Description, p.StartTime, p.EndTime })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = channels.Select(c => new
        {
            c.Id,
            c.ChannelName,
            c.StreamId,
            StreamIcon = c.CustomLogoPath is not null ? $"{baseUrl}{c.CustomLogoPath}" : c.StreamIcon,
            c.EpgChannelId,
            c.IsFavorite,
            StreamUrl = $"{opts.BaseUrl}/{opts.Username}/{opts.Password}/{c.StreamId}",
            c.CurrentProgram
        });

        return Ok(result);
    }

    /// <remarks>Returns details for a single movie by stream ID.</remarks>
    [HttpGet("{id}/movies/{streamId}")]
    public async Task<IActionResult> GetMovie(int id, int streamId)
    {
        var group = await db.ChannelGroups.FindAsync(id);
        if (group is null) return NotFound();
        if (group.Type != ChannelGroupType.VodMovies)
            return BadRequest("Group is not a VOD movies group.");

        VodInfoResponse info;
        try { info = await api.GetVodInfoAsync(streamId); }
        catch { return StatusCode(502, "Failed to fetch movie details from provider."); }

        var opts = apiOptions.Value;
        return Ok(new
        {
            info.Info,
            StreamUrl = info.MovieData?.StreamId is int sid
                ? opts.BuildStreamUrl("movie", sid, info.MovieData.ContainerExtension ?? "mp4")
                : null,
        });
    }

    /// <remarks>Returns all series belonging to a VOD series group, fetched live from the provider.</remarks>
    [HttpGet("{id}/series")]
    public async Task<IActionResult> GetSeries(int id)
    {
        var group = await db.ChannelGroups.FindAsync(id);
        if (group is null) return NotFound();
        if (group.Type != ChannelGroupType.VodSeries || group.VodCategoryId is null)
            return BadRequest("Group is not a VOD series group.");

        List<SeriesResponse> series;
        try { series = await api.GetSeriesAsync(group.VodCategoryId); }
        catch { return StatusCode(502, "Failed to fetch series from provider."); }

        return Ok(series.Select(s => new
        {
            s.Num,
            s.Name,
            s.SeriesId,
            Cover = s.Cover ?? string.Empty,
        }));
    }

    /// <remarks>Returns details for a single series by series ID, with seasons and episodes.</remarks>
    [HttpGet("{id}/series/{seriesId}")]
    public async Task<IActionResult> GetSeriesDetail(int id, int seriesId)
    {
        var group = await db.ChannelGroups.FindAsync(id);
        if (group is null) return NotFound();
        if (group.Type != ChannelGroupType.VodSeries)
            return BadRequest("Group is not a VOD series group.");

        SeriesInfoResponse info;
        try { info = await api.GetSeriesInfoAsync(seriesId); }
        catch { return StatusCode(502, "Failed to fetch series details from provider."); }

        var opts = apiOptions.Value;
        var episodesBySeasonKey = info.Episodes ?? [];
        var knownSeasonKeys = (info.Seasons ?? [])
            .Select(s => s.SeasonNumber?.ToString() ?? string.Empty)
            .ToHashSet();

        var mappedSeasons = (info.Seasons ?? []).Select(s =>
        {
            var key = s.SeasonNumber?.ToString() ?? string.Empty;
            var episodes = episodesBySeasonKey.TryGetValue(key, out var eps) ? eps : [];
            return (SeasonNumber: s.SeasonNumber ?? 0, Season: new
            {
                s.SeasonNumber,
                s.Name,
                s.AirDate,
                s.Overview,
                s.Cover,
                Episodes = MapEpisodes(episodes, opts),
            });
        });

        var orphanSeasons = episodesBySeasonKey
            .Where(kv => !knownSeasonKeys.Contains(kv.Key))
            .Select(kv =>
            {
                var seasonNumber = int.TryParse(kv.Key, out var n) ? n : 0;
                return (SeasonNumber: seasonNumber, Season: new
                {
                    SeasonNumber = (int?)seasonNumber,
                    Name = (string?)null,
                    AirDate = (string?)null,
                    Overview = (string?)null,
                    Cover = (string?)null,
                    Episodes = MapEpisodes(kv.Value, opts),
                });
            });

        var seasons = mappedSeasons
            .Concat(orphanSeasons)
            .OrderBy(s => s.SeasonNumber)
            .Select(s => s.Season);

        return Ok(new { info.Info, Seasons = seasons });
    }

    private static IEnumerable<object> MapEpisodes(IEnumerable<SeriesEpisode> episodes, ApiClientOptions opts) =>
        episodes
            .OrderBy(e => e.EpisodeNum)
            .Select(e => new
            {
                e.EpisodeNum,
                e.Title,
                e.ContainerExtension,
                StreamUrl = e.Id is not null
                    ? opts.BuildSeriesEpisodeUrl(e.Id, e.ContainerExtension ?? "mkv")
                    : null,
            });

    /// <remarks>Returns all movies belonging to a VOD movies group, fetched live from the provider.</remarks>
    [HttpGet("{id}/movies")]
    public async Task<IActionResult> GetMovies(int id)
    {
        var group = await db.ChannelGroups.FindAsync(id);
        if (group is null) return NotFound();
        if (group.Type != ChannelGroupType.VodMovies || group.VodCategoryId is null)
            return BadRequest("Group is not a VOD movies group.");

        List<VodStreamResponse> streams;
        try { streams = await api.GetVodStreamsAsync(group.VodCategoryId); }
        catch { return StatusCode(502, "Failed to fetch movies from provider."); }

        var opts = apiOptions.Value;
        return Ok(streams.Select(v => new
        {
            v.Num,
            v.Name,
            v.StreamId,
            StreamIcon = v.StreamIcon ?? string.Empty,
            StreamUrl = opts.BuildStreamUrl("movie", v.StreamId, v.ContainerExtension ?? "mp4"),
        }));
    }
}
