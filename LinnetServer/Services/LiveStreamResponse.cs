using System.Text.Json.Serialization;

namespace LinnetServer.Services;

public class LiveStreamResponse
{
    [JsonPropertyName("num")]
    public int Num { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("stream_id")]
    public int StreamId { get; set; }

    [JsonPropertyName("stream_icon")]
    public string StreamIcon { get; set; } = string.Empty;

    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("is_adult")]
    public int IsAdult { get; set; }

    [JsonPropertyName("epg_channel_id")]
    public string EpgChannelId { get; set; } = string.Empty;
}
