namespace LinnetServer.Services;

public class ApiClientOptions
{
    public const string SectionName = "ApiClient";
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
