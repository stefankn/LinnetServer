using LinnetServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LinnetServer.Controllers;

[ApiController]
[Route("api/v1/vod")]
public class VodController(IOptions<ApiClientOptions> apiOptions, IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet("download/{streamType}/{streamId}.{extension}")]
    public async Task<IActionResult> Download(string streamType, int streamId, string extension, CancellationToken ct)
    {
        var remoteUrl = apiOptions.Value.BuildStreamUrl(streamType, streamId, extension);
        if (string.IsNullOrEmpty(remoteUrl)) return BadRequest();

        var http = httpClientFactory.CreateClient("vod-download");
        var remoteResponse = await http.GetAsync(remoteUrl, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!remoteResponse.IsSuccessStatusCode)
            return StatusCode((int)remoteResponse.StatusCode);

        var contentType = remoteResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = $"{streamId}.{extension}";

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        if (remoteResponse.Content.Headers.ContentLength is { } length)
            Response.Headers.Append("Content-Length", length.ToString());

        var stream = await remoteResponse.Content.ReadAsStreamAsync(ct);
        return File(stream, contentType);
    }
}
