using System.Globalization;
using System.Xml.Linq;

namespace LinnetServer.Services;

public partial class XmltvEpgService(IHttpClientFactory httpClientFactory, ILogger<XmltvEpgService> logger)
{
    private const string FeedUrl = "https://raw.githubusercontent.com/stefankn/rakuten-uk-epg/master/epg.xml";
    private const string ClientName = "xmltv-epg";

    private Dictionary<string, List<XmltvProgram>>? _cache;
    private DateTime _cachedAt;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<List<XmltvProgram>> GetProgramsAsync(string epgChannelId, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_cache is null || DateTime.UtcNow - _cachedAt > CacheTtl)
            {
                LogFetchingFeed(logger, FeedUrl);
                _cache = await FetchAndParseAsync(ct);
                _cachedAt = DateTime.UtcNow;
                LogFeedParsed(logger, _cache.Count);
            }
            return _cache.TryGetValue(epgChannelId, out var programs) ? programs : [];
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Dictionary<string, List<XmltvProgram>>> FetchAndParseAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        await using var stream = await client.GetStreamAsync(FeedUrl, ct);
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);

        var result = new Dictionary<string, List<XmltvProgram>>(StringComparer.OrdinalIgnoreCase);

        foreach (var programme in doc.Root!.Elements("programme"))
        {
            var channelId = (string?)programme.Attribute("channel");
            var startRaw = (string?)programme.Attribute("start");
            var stopRaw = (string?)programme.Attribute("stop");
            var title = programme.Element("title")?.Value;
            var desc = programme.Element("desc")?.Value ?? string.Empty;

            if (channelId is null || startRaw is null || stopRaw is null || title is null)
                continue;

            if (!TryParseXmltvTimestamp(startRaw, out var start) || !TryParseXmltvTimestamp(stopRaw, out var stop))
                continue;

            if (!result.TryGetValue(channelId, out var list))
            {
                list = [];
                result[channelId] = list;
            }
            list.Add(new XmltvProgram(channelId, title, desc, start, stop));
        }

        return result;
    }

    private static bool TryParseXmltvTimestamp(string raw, out DateTime result)
    {
        result = default;
        // Format: "20260606090001 +0200" — normalize offset to "+02:00" for zzz specifier
        var spaceIndex = raw.IndexOf(' ');
        if (spaceIndex < 0) return false;
        var datePart = raw[..spaceIndex];
        var offsetPart = raw[(spaceIndex + 1)..];
        if (offsetPart.Length != 5) return false;
        var normalizedOffset = offsetPart.Insert(3, ":");
        if (!DateTimeOffset.TryParseExact(
                $"{datePart} {normalizedOffset}",
                "yyyyMMddHHmmss zzz",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dto))
            return false;
        result = dto.UtcDateTime;
        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching XMLTV EPG feed from {Url}")]
    private static partial void LogFetchingFeed(ILogger logger, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "XMLTV EPG feed parsed: {ChannelCount} channel(s) found")]
    private static partial void LogFeedParsed(ILogger logger, int channelCount);
}

public record XmltvProgram(string ChannelId, string Title, string Description, DateTime StartTime, DateTime EndTime);
