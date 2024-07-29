using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;

namespace TheDialgaTeam.Worktips.Explorer.Server.Database.Repositories;

public sealed class FaucetHistoryRepository(IDbContextFactory<SqliteDatabaseContext> dbContextFactory)
{
    public bool IsFaucetClaimable(ulong userId, out TimeSpan duration)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var faucetHistory = dbContext.FaucetHistories.SingleOrDefault(history => history.UserId == userId);

        if (faucetHistory is null)
        {
            duration = TimeSpan.Zero;
            return true;
        }

        duration = DateTimeOffset.Now - faucetHistory.DateTime;
        return duration >= TimeSpan.FromHours(1);
    }

    public async Task SetFaucetClaimedAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var faucetHistory = await dbContext.FaucetHistories.SingleOrDefaultAsync(history => history.UserId == userId, cancellationToken).ConfigureAwait(false);

        if (faucetHistory is null)
        {
            dbContext.FaucetHistories.Add(new FaucetHistory { UserId = userId, DateTime = DateTimeOffset.Now });
        }
        else
        {
            faucetHistory.DateTime = DateTimeOffset.Now;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}