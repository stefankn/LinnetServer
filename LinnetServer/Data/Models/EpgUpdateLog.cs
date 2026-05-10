namespace LinnetServer.Data.Models;

public class EpgUpdateLog
{
    public int Id { get; set; }
    public int ChannelGroupItemId { get; set; }
    public DateTime Timestamp { get; set; }
    public EpgUpdateStatus Status { get; set; }
    public EpgUpdateTrigger Trigger { get; set; }
    public int? ProgramCount { get; set; }
    public string? ErrorMessage { get; set; }

    public ChannelGroupItem ChannelGroupItem { get; set; } = null!;
}

public enum EpgUpdateStatus { Started, Completed, Failed }
public enum EpgUpdateTrigger { Manual, Nightly, Startup }
