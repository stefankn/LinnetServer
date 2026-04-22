using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LinnetServer.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiClientOptions _options;

    public ApiClient(HttpClient httpClient, IOptions<ApiClientOptions> options)
    {
        _options = options.Value;
        httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient = httpClient;
    }

    public async Task<AccountInfoResponse> GetAccountInfoAsync()
    {
        var url = $"/player_api.php?username={Uri.EscapeDataString(_options.Username)}&password={Uri.EscapeDataString(_options.Password)}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var wrapper = JsonSerializer.Deserialize<AccountInfoApiResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize account info response.");
        return wrapper.UserInfo;
    }

    public async Task<List<LiveCategoryResponse>> GetLiveCategoriesAsync()
    {
        var url = $"/player_api.php?username={Uri.EscapeDataString(_options.Username)}&password={Uri.EscapeDataString(_options.Password)}&action=get_live_categories";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<LiveCategoryResponse>>(json)
            ?? throw new InvalidOperationException("Failed to deserialize live categories response.");
    }

    public async Task<List<LiveStreamResponse>> GetLiveStreamsAsync(string categoryId)
    {
        var url = $"/player_api.php?username={Uri.EscapeDataString(_options.Username)}&password={Uri.EscapeDataString(_options.Password)}&action=get_live_streams&category_id={Uri.EscapeDataString(categoryId)}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<LiveStreamResponse>>(json)
            ?? throw new InvalidOperationException("Failed to deserialize live streams response.");
    }

    public async Task<List<EpgListingResponse>> GetEpgGuideAsync(int streamId)
    {
        var url = $"/player_api.php?username={Uri.EscapeDataString(_options.Username)}&password={Uri.EscapeDataString(_options.Password)}&action=get_simple_data_table&stream_id={streamId}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        try
        {
            var wrapper = JsonSerializer.Deserialize<EpgGuideApiResponse>(json);
            return wrapper?.EpgListings ?? [];
        }
        catch (JsonException)
        {
            // API returns `false` or other non-object JSON when no EPG data is available
            return [];
        }
    }
}
