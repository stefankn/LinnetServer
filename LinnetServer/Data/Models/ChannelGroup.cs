namespace LinnetServer.Data.Models;

public class ChannelGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChannelGroupItem> Items { get; set; } = [];
}
