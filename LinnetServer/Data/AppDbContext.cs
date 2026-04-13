using LinnetServer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LinnetServer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ChannelGroup> ChannelGroups => Set<ChannelGroup>();
    public DbSet<ChannelGroupItem> ChannelGroupItems => Set<ChannelGroupItem>();
}
