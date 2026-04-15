using LinnetServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinnetServer.Controllers;

[ApiController]
[Route("api/v1/groups")]
public class GroupsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetGroups() =>
        Ok(await db.ChannelGroups
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync());

    [HttpGet("{id}/channels")]
    public async Task<IActionResult> GetChannels(int id)
    {
        if (!await db.ChannelGroups.AnyAsync(g => g.Id == id))
            return NotFound();

        var channels = await db.ChannelGroupItems
            .Where(c => c.ChannelGroupId == id)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.ChannelName, c.StreamId, c.StreamIcon, c.EpgChannelId })
            .ToListAsync();

        return Ok(channels);
    }
}
