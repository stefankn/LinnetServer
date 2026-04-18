using System.Text.Json;
using System.Text.RegularExpressions;

namespace LinnetServer.Services;

public partial class TvLogoService
{
    private readonly string _indexPath;
    private readonly string _logosDir;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TvLogoService> _logger;

    private volatile IReadOnlyList<TvLogoEntry> _index = [];

    public TvLogoService(IWebHostEnvironment env, IHttpClientFactory httpClientFactory, ILogger<TvLogoService> logger)
    {
        _logosDir = Path.Combine(env.WebRootPath, "logos");
        _indexPath = Path.Combine(_logosDir, "logo-index.json");
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        Directory.CreateDirectory(_logosDir);
        LoadIndexFromFile();
    }

    public bool IsIndexLoaded => _index.Count > 0;

    public bool IsIndexFresh =>
        File.Exists(_indexPath) &&
        (DateTime.UtcNow - File.GetLastWriteTimeUtc(_indexPath)) < TimeSpan.FromDays(7);

    public IReadOnlyList<TvLogoEntry> Search(string query, int maxResults = 24)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var slug = Slugify(query);
        if (string.IsNullOrEmpty(slug)) return [];

        var index = _index;
        return index
            .Where(e => e.Slug.Contains(slug))
            .OrderBy(e => e.Slug == slug ? 0 : 1)
            .ThenBy(e => e.Slug.Length)
            .Take(maxResults)
            .ToList();
    }

    public TvLogoEntry? AutoMatch(string channelName, string? preferredCountry = null)
    {
        var slug = Slugify(channelName);
        if (string.IsNullOrEmpty(slug)) return null;

        var exactMatches = _index.Where(e => e.Slug == slug).ToList();
        if (exactMatches.Count == 0) return null;
        if (exactMatches.Count == 1) return exactMatches[0];

        if (preferredCountry is not null)
            return exactMatches.FirstOrDefault(e => e.CountryCode == preferredCountry);

        return null;
    }

    public async Task<string> DownloadLogoAsync(TvLogoEntry entry)
    {
        var localPath = Path.Combine(_logosDir, entry.FileName);
        if (!File.Exists(localPath))
        {
            var http = _httpClientFactory.CreateClient("tvlogos");
            var bytes = await http.GetByteArrayAsync(entry.RawUrl);
            await File.WriteAllBytesAsync(localPath, bytes);
        }
        return $"/logos/{entry.FileName}";
    }

    public async Task RefreshIndexAsync(CancellationToken ct = default)
    {
        LogRefreshStarted(_logger);

        var http = _httpClientFactory.CreateClient("tvlogos");
        var response = await http.GetAsync(
            "https://api.github.com/repos/tv-logo/tv-logos/git/trees/main?recursive=1", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var entries = new List<TvLogoEntry>();
        foreach (var item in doc.RootElement.GetProperty("tree").EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "blob") continue;

            var path = item.GetProperty("path").GetString();
            if (path is null) continue;
            if (!path.StartsWith("countries/")) continue;
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

            var entry = ParseEntry(path);
            if (entry is not null)
                entries.Add(entry);
        }

        await File.WriteAllTextAsync(_indexPath,
            JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = false }), ct);

        _index = entries;
        LogRefreshComplete(_logger, entries.Count);
    }

    private void LoadIndexFromFile()
    {
        if (!File.Exists(_indexPath)) return;
        try
        {
            var json = File.ReadAllText(_indexPath);
            var entries = JsonSerializer.Deserialize<List<TvLogoEntry>>(json);
            if (entries is not null)
            {
                _index = entries;
                LogIndexLoaded(_logger, entries.Count);
            }
        }
        catch (Exception ex)
        {
            LogIndexLoadFailed(_logger, ex);
        }
    }

    private static TvLogoEntry? ParseEntry(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var parts = fileName.Split('-');
        if (parts.Length < 2) return null;

        var countryCode = parts[^1];
        if (countryCode.Length != 2) return null;

        var slug = string.Join('-', parts[..^1]);
        return new TvLogoEntry(path, slug, countryCode);
    }

    internal static string Slugify(string name)
    {
        name = name.ToLowerInvariant();
        name = SuffixRegex().Replace(name, "");
        name = NonAlphanumericRegex().Replace(name, " ");
        name = WhitespaceRegex().Replace(name.Trim(), "-");
        return name.Trim('-');
    }

    [GeneratedRegex(@"\b(hd|4k|fhd|uhd|sd)\b")]
    private static partial Regex SuffixRegex();

    [GeneratedRegex(@"[^a-z0-9\s\-]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [LoggerMessage(Level = LogLevel.Information, Message = "Refreshing tv-logo index from GitHub")]
    private static partial void LogRefreshStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "tv-logo index refreshed: {Count} logos indexed")]
    private static partial void LogRefreshComplete(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded tv-logo index from cache: {Count} logos")]
    private static partial void LogIndexLoaded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load tv-logo index from cache")]
    private static partial void LogIndexLoadFailed(ILogger logger, Exception ex);
}
