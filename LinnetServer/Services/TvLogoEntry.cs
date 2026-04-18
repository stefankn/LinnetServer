namespace LinnetServer.Services;

public record TvLogoEntry(string Path, string Slug, string CountryCode)
{
    public string RawUrl =>
        $"https://raw.githubusercontent.com/tv-logo/tv-logos/main/{Path}";

    public string FileName => System.IO.Path.GetFileName(Path);
}
