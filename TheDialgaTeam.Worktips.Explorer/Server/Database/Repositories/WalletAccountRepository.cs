using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Wallet;
using TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;

namespace TheDialgaTeam.Worktips.Explorer.Server.Database.Repositories;

public sealed class WalletAccountRepository(
    IDbContextFactory<SqliteDatabaseContext> dbContextFactory,
    WalletRpcClient walletRpcClient)
{
    public async Task<WalletAccount> GetOrCreateWalletAccountAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var result = await dbContext.WalletAccounts.SingleOrDefaultAsync(account => account.UserId == userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result != null) return result;

        var newWallet = await walletRpcClient.CreateAccountAsync(new CommandRpcCreateAccount.Request { Label = userId.ToString() }, cancellationToken).ConfigureAwait(false);
        if (newWallet == null) throw new Exception();

        var walletAccount = new WalletAccount
        {
            UserId = userId,
            RegisteredWalletAddress = null,
            AccountIndex = newWallet.AccountIndex,
            TipBotWalletAddress = newWallet.Address,
            SentToRegisteredWalletDirectly = false
        };

        dbContext.WalletAccounts.Add(walletAccount);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return walletAccount;
    }

    public async Task UpdateWalletAccountAsync(ulong userId, Action<WalletAccount> updateQuery, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var result = await dbContext.WalletAccounts.SingleOrDefaultAsync(account => account.UserId == userId, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result != null)
        {
            updateQuery(result);
        }
        else
        {
            var newWallet = await walletRpcClient.CreateAccountAsync(new CommandRpcCreateAccount.Request { Label = userId.ToString() }, cancellationToken).ConfigureAwait(false);
            if (newWallet == null) throw new Exception();

            var walletAccount = new WalletAccount
            {
                UserId = userId,
                RegisteredWalletAddress = null,
                AccountIndex = newWallet.AccountIndex,
                TipBotWalletAddress = newWallet.Address,
                SentToRegisteredWalletDirectly = false
            };

            updateQuery(walletAccount);

            dbContext.WalletAccounts.Add(walletAccount);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}