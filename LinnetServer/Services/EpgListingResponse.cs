using System.Text.Json.Serialization;

namespace LinnetServer.Services;

public class EpgGuideApiResponse
{
    [JsonPropertyName("epg_listings")]
    public List<EpgListingResponse> EpgListings { get; set; } = [];
}

public class EpgListingResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("epg_id")]
    public string EpgId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("channel_id")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("start_timestamp")]
    public string StartTimestamp { get; set; } = string.Empty;

    [JsonPropertyName("end_timestamp")]
    public string EndTimestamp { get; set; } = string.Empty;
}
