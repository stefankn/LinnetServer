using LinnetServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LinnetServer.Controllers;

[ApiController]
[Route("api/v1/series")]
public class SeriesController(IOptions<ApiClientOptions> apiOptions, IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet("download/{episodeId}.{containerExtension}")]
    public async Task<IActionResult> Download(string episodeId, string containerExtension, [FromQuery] string? name, CancellationToken ct)
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

    private static string SanitizeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    }
}
