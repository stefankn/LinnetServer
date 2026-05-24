using LinnetServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LinnetServer.Controllers;

[ApiController]
[Route("api/v1/vod")]
public class VodController(IOptions<ApiClientOptions> apiOptions, ApiClient api, IHttpClientFactory httpClientFactory) : ControllerBase
{
    /// <remarks>Returns details for a single movie by stream ID.</remarks>
    [HttpGet("movies/{streamId}")]
    public async Task<IActionResult> GetMovieInfo(int streamId)
    {
        VodInfoResponse info;
        try { info = await api.GetVodInfoAsync(streamId); }
        catch { return StatusCode(502, "Failed to fetch movie details from provider."); }

        var opts = apiOptions.Value;
        var sid = info.MovieData?.StreamId;
        return Ok(new
        {
            info.Info,
            StreamId = sid,
            CoverUrl = info.Info?.CoverBig ?? info.Info?.MovieImage,
            StreamUrl = sid is int id
                ? opts.BuildStreamUrl("movie", id, info.MovieData!.ContainerExtension ?? "mp4")
                : null,
        });
    }

    /// <remarks>Returns details for a single series by series ID, with seasons and episodes.</remarks>
    [HttpGet("series/{seriesId}")]
    public async Task<IActionResult> GetSeriesInfo(int seriesId)
    {
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

        return Ok(new { SeriesId = seriesId, info.Info, Seasons = seasons });
    }

    private static IEnumerable<object> MapEpisodes(IEnumerable<SeriesEpisode> episodes, ApiClientOptions opts) =>
        episodes
            .OrderBy(e => e.EpisodeNum)
            .Select(e => new
            {
                e.Id,
                e.EpisodeNum,
                e.Title,
                e.ContainerExtension,
                StreamUrl = e.Id is not null
                    ? opts.BuildSeriesEpisodeUrl(e.Id, e.ContainerExtension ?? "mkv")
                    : null,
            });

    [HttpGet("series/download/{episodeId}.{containerExtension}")]
    public async Task<IActionResult> DownloadEpisode(string episodeId, string containerExtension, [FromQuery] string? name, CancellationToken ct)
    {
        var remoteUrl = apiOptions.Value.BuildSeriesEpisodeUrl(episodeId, containerExtension);
        if (string.IsNullOrEmpty(remoteUrl)) return BadRequest();

        var http = httpClientFactory.CreateClient("vod-download");
        var remoteResponse = await http.GetAsync(remoteUrl, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!remoteResponse.IsSuccessStatusCode)
            return StatusCode((int)remoteResponse.StatusCode);

        var contentType = remoteResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var baseName = SanitizeFileName(name) is { Length: > 0 } n ? n : episodeId;
        var fileName = $"{baseName}.{containerExtension}";

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        if (remoteResponse.Content.Headers.ContentLength is { } length)
            Response.Headers.Append("Content-Length", length.ToString());

        var stream = await remoteResponse.Content.ReadAsStreamAsync(ct);
        return File(stream, contentType);
    }

    [HttpGet("download/{streamType}/{streamId}.{extension}")]
    public async Task<IActionResult> Download(string streamType, int streamId, string extension, [FromQuery] string? name, CancellationToken ct)
    {
        var remoteUrl = apiOptions.Value.BuildStreamUrl(streamType, streamId, extension);
        if (string.IsNullOrEmpty(remoteUrl)) return BadRequest();

        var http = httpClientFactory.CreateClient("vod-download");
        var remoteResponse = await http.GetAsync(remoteUrl, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!remoteResponse.IsSuccessStatusCode)
            return StatusCode((int)remoteResponse.StatusCode);

        var contentType = remoteResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var baseName = SanitizeFileName(name) is { Length: > 0 } n ? n : streamId.ToString();
        var fileName = $"{baseName}.{extension}";

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        if (remoteResponse.Content.Headers.ContentLength is { } length)
            Response.Headers.Append("Content-Length", length.ToString());

        var stream = await remoteResponse.Content.ReadAsStreamAsync(ct);
        return File(stream, contentType);
    }

    private static string SanitizeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    }
}
