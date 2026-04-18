namespace LinnetServer.Data.Models;

public class ChannelGroupItem
{
    public int Id { get; set; }
    public int ChannelGroupId { get; set; }
    public int StreamId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public string StreamIcon { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public bool IsAdult { get; set; }
    public string EpgChannelId { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public string? CustomLogoPath { get; set; }

    public string EffectiveLogoUrl => CustomLogoPath ?? StreamIcon;

    public DateTime? EpgLastUpdated { get; set; }

    public ChannelGroup ChannelGroup { get; set; } = null!;
    public ICollection<ChannelProgram> Programs { get; set; } = [];
}
