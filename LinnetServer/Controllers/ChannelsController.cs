using LinnetServer.Data;
using LinnetServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinnetServer.Controllers;

[ApiController]
[Route("api/v1/channels")]
public class ChannelsController(AppDbContext db, IOptions<ApiClientOptions> apiOptions) : ControllerBase
{
    /// <remarks>Returns all favorite channels.</remarks>
    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
    {
        var opts = apiOptions.Value;
        var now = DateTime.UtcNow;
        var channels = await db.ChannelGroupItems
            .Where(c => c.IsFavorite)
            .OrderBy(c => c.ChannelName)
            .Select(c => new
            {
                c.Id,
                c.ChannelName,
                c.StreamId,
                c.StreamIcon,
                c.CustomLogoPath,
                c.EpgChannelId,
                c.ChannelGroupId,
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
            c.ChannelGroupId,
            c.IsFavorite,
            StreamUrl = $"{opts.BaseUrl}/{opts.Username}/{opts.Password}/{c.StreamId}",
            c.CurrentProgram
        });

        return Ok(result);
    }


    /// <remarks>Sets the favorite status of a channel.</remarks>
    [HttpPut("{id}/favorite")]
    public async Task<IActionResult> SetFavorite(int id, [FromBody] SetFavoriteRequest request)
    {
        var channel = await db.ChannelGroupItems.FindAsync(id);
        if (channel is null)
            return NotFound();

        channel.IsFavorite = request.IsFavorite;
        await db.SaveChangesAsync();
        return NoContent();
    }

    public record SetFavoriteRequest(bool IsFavorite);

    /// <remarks>Returns the EPG guide for a channel for today and the next 2 days.</remarks>
    [HttpGet("{id}/guide")]
    public async Task<IActionResult> GetPrograms(int id)
    {
        if (!await db.ChannelGroupItems.AnyAsync(c => c.Id == id))
            return NotFound();

        var from = DateTime.UtcNow.Date;
        var to = from.AddDays(3);

        var programs = await db.ChannelPrograms
            .Where(p => p.ChannelGroupItemId == id && p.StartTime >= from && p.StartTime < to)
            .OrderBy(p => p.StartTime)
            .Select(p => new { p.Id, p.Title, p.Description, p.StartTime, p.EndTime })
            .ToListAsync();

        return Ok(programs);
    }

    /// <remarks>Returns the airing program for a channel at a given time. Defaults to now if no date is provided.</remarks>
    [HttpGet("{id}/guide/program")]
    public async Task<IActionResult> GetCurrentProgram(int id, [FromQuery] DateTimeOffset? at)
    {
        if (!await db.ChannelGroupItems.AnyAsync(c => c.Id == id))
            return NotFound();

        var time = at.HasValue ? at.Value.UtcDateTime : DateTime.UtcNow;
        var program = await db.ChannelPrograms
            .Where(p => p.ChannelGroupItemId == id && p.StartTime <= time && p.EndTime >= time)
            .Select(p => new { p.Id, p.Title, p.Description, p.StartTime, p.EndTime })
            .FirstOrDefaultAsync();

        return program is null ? NoContent() : Ok(program);
    }
}
