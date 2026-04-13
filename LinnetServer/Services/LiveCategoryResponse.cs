using System.Text.Json.Serialization;

namespace LinnetServer.Services;

public class LiveCategoryResponse
{
    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; } = string.Empty;
}
