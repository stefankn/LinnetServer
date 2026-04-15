using LinnetServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinnetServer.Controllers;

[ApiController]
[Route("api/v1/channels")]
public class ChannelsController(AppDbContext db) : ControllerBase
{
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
}
