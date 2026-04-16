using LinnetServer.Data;
using LinnetServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinnetServer.Controllers;

[ApiController]
[Route("api/v1/groups")]
public class GroupsController(AppDbContext db, IOptions<ApiClientOptions> apiOptions) : ControllerBase
{
    /// <remarks>Returns all channel groups.</remarks>
    [HttpGet]
    public async Task<IActionResult> GetGroups() =>
        Ok(await db.ChannelGroups
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name })
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
                c.EpgChannelId,
                c.SortOrder,
                CurrentProgram = c.Programs
                    .Where(p => p.StartTime <= now && p.EndTime >= now)
                    .Select(p => new { p.Id, p.Title, p.Description, p.StartTime, p.EndTime })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var result = channels.Select(c => new
        {
            c.Id,
            c.ChannelName,
            c.StreamId,
            c.StreamIcon,
            c.EpgChannelId,
            StreamUrl = $"{opts.BaseUrl}/{opts.Username}/{opts.Password}/{c.StreamId}",
            c.CurrentProgram
        });

        return Ok(result);
    }
}
