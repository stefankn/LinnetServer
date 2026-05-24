namespace LinnetServer.Data.Models;

public enum WatchProgressContentType { Movie, Episode }

public class WatchProgress
{
    public int Id { get; set; }
    public WatchProgressContentType ContentType { get; set; }
    public string StreamId { get; set; } = string.Empty; // movie stream_id (int→string) or episode id (string)
    public string Title { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public int? SeriesId { get; set; }      // episodes only — for navigation back to series
    public int? SeasonNumber { get; set; }  // episodes only
    public int? EpisodeNumber { get; set; } // episodes only
    public int PositionSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
