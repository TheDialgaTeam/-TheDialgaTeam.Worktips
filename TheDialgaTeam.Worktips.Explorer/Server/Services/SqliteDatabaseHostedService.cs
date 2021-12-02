using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Worktips.Explorer.Server.Database;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

public class SqliteDatabaseHostedService : IHostedService
{
    private readonly IDbContextFactory<SqliteDatabaseContext> _contextFactory;

    public SqliteDatabaseHostedService(IDbContextFactory<SqliteDatabaseContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var sqliteDatabaseContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await sqliteDatabaseContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}