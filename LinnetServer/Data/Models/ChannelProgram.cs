namespace LinnetServer.Data.Models;

public class ChannelProgram
{
    public int Id { get; set; }
    public int ChannelGroupItemId { get; set; }
    public string EpgId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public ChannelGroupItem ChannelGroupItem { get; set; } = null!;
}
