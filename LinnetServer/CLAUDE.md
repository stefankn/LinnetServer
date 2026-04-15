# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run development server (http://localhost:5188, https://localhost:7047)
dotnet run

# Build
dotnet build

# Database migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update

# Docker (includes PostgreSQL)
docker compose up
```

Frontend assets (Tailwind CSS + esbuild JS bundling) are compiled automatically as MSBuild pre-build targets — no manual npm commands needed during development.

## Architecture

LinnetServer is an ASP.NET Core 10 application for managing IPTV channel groups and EPG (Electronic Program Guide) data. It combines a Blazor Interactive Server frontend with a REST API.

### Key layers

**Blazor UI** (`Components/Pages/`) — Server-side interactive components for managing channel groups (`ChannelGroups.razor`), viewing channel/program listings (`ChannelDetail.razor`), and a dashboard (`Home.razor`). Uses Tailwind CSS + DaisyUI with dark theme toggling.

**REST API** (`Controllers/`) — Two controllers expose channel/group data for external consumers (e.g. media players). API documentation is available via Scalar at `/scalar/v1`.

**Data layer** (`Data/`) — EF Core 9 with PostgreSQL. Three entities: `ChannelGroup`, `ChannelGroupItem` (a channel within a group, with EPG channel ID and stream URL), and `ChannelProgram` (program entries with start/end times). Connection string is configured in `appsettings.json`.

**Services** (`Services/`) — `ApiClient` fetches data from an external IPTV provider API using URL-based auth. EPG titles and descriptions from the provider are base64-encoded and decoded in the service layer.

**Background workers** (`Services/`) — Two `BackgroundService` implementations:
- `EpgWorker` — consumes from `EpgUpdateQueue`, fetches programs for each channel in batches of 100, retries up to 3 times with exponential backoff (5s/15s/30s).
- `EpgRefreshWorker` — runs daily at 3:00 AM, queues channels whose EPG data is older than 6 days.

`EpgUpdateQueue` uses `System.Threading.Channels` for async producer/consumer and `ConcurrentDictionary` to prevent duplicate enqueuing. It exposes an `OnChanged` event that Blazor components subscribe to for real-time queue status updates.

### Configuration

- `appsettings.Development.json` — local PostgreSQL (`Host=localhost;Port=5432;Database=linnet;Username=linnet;Password=linnet`) and IPTV API credentials
- `appsettings.json` — production template; connection string and API credentials are empty and must be supplied at runtime
- Logging via Serilog with Console + rotating File sinks (14-day retention in production, 7-day in development)

### Frontend build pipeline

Defined as MSBuild targets in `LinnetServer.csproj`. Source files are `Components/app.css` (Tailwind input) and `Components/app.js` (esbuild entry point); compiled output goes to `wwwroot/`. Release builds minify both outputs.
