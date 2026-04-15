# LinnetServer

A self-hosted IPTV management server built with ASP.NET Core 10 and Blazor. Organize channels into groups, browse program schedules, and keep EPG data refreshed automatically.

## Features

- **Channel Group Management** — Curate channels from your IPTV provider into named groups
- **EPG / Program Guide** — Browse program schedules with automatic daily refresh at 3 AM
- **REST API** — Expose channel lists and EPG data to media players or other clients
- **Blazor UI** — Interactive server-side UI with dark mode support
- **Docker Support** — One-command deployment with PostgreSQL included

## Tech Stack

- **Backend**: ASP.NET Core 10, Entity Framework Core 9, PostgreSQL 17
- **Frontend**: Blazor Interactive Server, Tailwind CSS v4, DaisyUI v5
- **Bundling**: esbuild (JS), Tailwind CLI (CSS)
- **Logging**: Serilog (rotating daily log files)
- **API Docs**: Scalar (`/scalar/v1` in development)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js / npm (for CSS/JS build tooling)
- PostgreSQL 17 (or use Docker Compose)

## Getting Started

### Configuration

Copy `.env.example` to `.env` and fill in your values:

```bash
cp .env.example .env
```

```ini
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=linnet;Username=linnet;Password=linnet
ApiClient__BaseUrl=http://your-iptv-provider.example.com
ApiClient__Username=your-username
ApiClient__Password=your-password
```

The `.env` file is gitignored and never committed.

### Run Locally

```bash
# Apply database migrations
dotnet ef database update

# Start the development server
dotnet run
```

The app will be available at `http://localhost:5188` (HTTP) or `https://localhost:7047` (HTTPS).

API documentation is available at `/scalar/v1`.

### Run with Docker

```bash
cp .env.example .env
# Edit .env with your credentials, then:
docker compose up
```

The compose file passes the `.env` variables into the container. Note that `Host` in the connection string should be `postgres` (the compose service name) instead of `localhost`.

## REST API

The API is versioned under `/api/v1` and intended for media players or external clients.

### Groups

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/groups` | List all channel groups |
| `GET` | `/api/v1/groups/{id}/channels` | List channels in a group (includes stream URLs) |

### Channels

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/channels/{id}/guide` | EPG guide for a channel (today + next 2 days) |

## Project Structure

```
LinnetServer/
├── Components/          # Blazor pages and UI components
├── Controllers/         # REST API controllers
├── Data/                # EF Core DbContext and entity models
├── Services/            # Business logic, API client, EPG workers
├── Migrations/          # EF Core database migrations
├── wwwroot/             # Compiled static assets
├── Dockerfile
├── compose.yaml
└── Program.cs           # App startup and DI configuration
```

## Database Migrations

```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Apply pending migrations
dotnet ef database update
```

## Background Workers

- **EpgWorker** — Processes queued EPG updates in batches (with retry logic)
- **EpgRefreshWorker** — Runs daily at 3 AM, refreshing channels with stale EPG data (older than 6 days)
