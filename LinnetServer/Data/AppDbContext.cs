using LinnetServer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LinnetServer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ChannelGroup> ChannelGroups => Set<ChannelGroup>();
    public DbSet<ChannelGroupItem> ChannelGroupItems => Set<ChannelGroupItem>();
    public DbSet<ChannelProgram> ChannelPrograms => Set<ChannelProgram>();
    public DbSet<EpgUpdateLog> EpgUpdateLogs => Set<EpgUpdateLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChannelProgram>()
            .HasOne(p => p.ChannelGroupItem)
            .WithMany(i => i.Programs)
            .HasForeignKey(p => p.ChannelGroupItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EpgUpdateLog>()
            .HasOne(l => l.ChannelGroupItem)
            .WithMany()
            .HasForeignKey(l => l.ChannelGroupItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EpgUpdateLog>()
            .HasIndex(l => l.ChannelGroupItemId);
    }
}
