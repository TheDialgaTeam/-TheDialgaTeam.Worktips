using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;

namespace TheDialgaTeam.Worktips.Explorer.Server.Database;

public class SqliteDatabaseContext : DbContext
{
    public DbSet<WalletAccount> WalletAccounts { get; set; } = null!;

    public DbSet<FaucetHistory> FaucetHistories { get; set; } = null!;

    public DbSet<DaemonSyncHistory> DaemonSyncHistory { get; set; } = null!;

    public SqliteDatabaseContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DaemonSyncHistory>().Property(typeof(int), "Id");
        modelBuilder.Entity<DaemonSyncHistory>().HasKey("Id");
        modelBuilder.Entity<DaemonSyncHistory>().HasData(new
        {
            Id = 1,
            BlockCount = 0ul,
            TotalCirculation = 0ul
        });
    }
}