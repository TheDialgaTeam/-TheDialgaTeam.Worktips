using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;

namespace TheDialgaTeam.Worktips.Explorer.Server.Database;

public sealed class SqliteDatabaseContext(IHostEnvironment hostEnvironment) : DbContext
{
    public required DbSet<WalletAccount> WalletAccounts { get; set; }

    public required DbSet<FaucetHistory> FaucetHistories { get; set; }

    public required DbSet<DaemonSyncHistory> DaemonSyncHistory { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DaemonSyncHistory>().HasData(new
        {
            Id = 1,
            BlockCount = 0ul,
            TotalCirculation = 0ul
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(hostEnvironment.ContentRootPath, "data.db")
        };
        
        optionsBuilder.UseSqlite(builder.ConnectionString);
    }
}