using System.Security.Cryptography;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Wallet;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Shared;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

internal sealed class FaucetModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly DiscordOptions _discordOptions;
    private readonly BlockchainOptions _blockchainOptions;
    private readonly IDbContextFactory<SqliteDatabaseContext> _dbContextFactory;
    private readonly WalletRpcClient _walletRpcClient;
    
    public FaucetModule(IOptions<DiscordOptions> discordOptions, IOptions<BlockchainOptions> blockchainOptions, IDbContextFactory<SqliteDatabaseContext> dbContextFactory, WalletRpcClient walletRpcClient)
    {
        _discordOptions = discordOptions.Value;
        _blockchainOptions = blockchainOptions.Value;
        _dbContextFactory = dbContextFactory;
        _walletRpcClient = walletRpcClient;
    }
    
    private static async Task<WalletAccount> GetOrCreateWalletAccountAsync(ulong userId, IDbContextFactory<SqliteDatabaseContext> dbContextFactory, WalletRpcClient walletRpcClient)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var result = await dbContext.WalletAccounts.SingleOrDefaultAsync(account => account.UserId == userId).ConfigureAwait(false);
        if (result != null) return result;

        var newWallet = await walletRpcClient.CreateAccountAsync(new CommandRpcCreateAccount.Request { Label = userId.ToString() }).ConfigureAwait(false);
        if (newWallet == null) throw new Exception();

        var walletAccount = new WalletAccount
        {
            UserId = userId,
            RegisteredWalletAddress = null,
            AccountIndex = newWallet.AccountIndex,
            TipBotWalletAddress = newWallet.Address
        };

        dbContext.WalletAccounts.Add(walletAccount);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        return walletAccount;
    }

    [SlashCommand("faucet", "Get a small tip from the faucet.", true)]
    public async Task FaucetCommand()
    {
        await DeferAsync().ConfigureAwait(false);
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var faucetHistory = await dbContext.FaucetHistories.SingleOrDefaultAsync(history => history.UserId == Context.User.Id).ConfigureAwait(false);

        if (faucetHistory != null && DateTimeOffset.Now - faucetHistory.DateTime < TimeSpan.FromHours(1))
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription($"You have to wait for {(faucetHistory.DateTime.AddHours(1) - DateTimeOffset.Now).TotalMinutes:N0} minutes before you can receive tips again.")
                .Build()).ConfigureAwait(false);
            
            return;
        }

        var botWalletAccount = await GetOrCreateWalletAccountAsync(Context.Client.CurrentUser.Id, _dbContextFactory, _walletRpcClient).ConfigureAwait(false);
        var botBalanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = botWalletAccount.AccountIndex }).ConfigureAwait(false);
        if (botBalanceResponse == null) throw new Exception();

        if (botBalanceResponse.Balance <= _discordOptions.Modules.Faucet.Amounts.Max(amount => amount.Amount))
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription($"The faucet requires a minimum of {DaemonUtility.FormatAtomicUnit(_discordOptions.Modules.Faucet.Amounts.Max(amount => amount.Amount), _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} in the bot balance. Please consider donating to keep this faucet running :)")
                .Build()).ConfigureAwait(false);
            
            return;
        }

        var randomRewardWeight = RandomNumberGenerator.GetInt32(0, _discordOptions.Modules.Faucet.Amounts.Sum(amount => amount.Weight));
        var randomRewardWeightTotal = 0;
        var atomicAmountToTip = 0ul;

        foreach (var faucetAmount in _discordOptions.Modules.Faucet.Amounts)
        {
            if (randomRewardWeight < faucetAmount.Weight + randomRewardWeightTotal)
            {
                atomicAmountToTip = faucetAmount.Amount;
                break;
            }

            randomRewardWeightTotal += faucetAmount.Weight;
        }

        if (botBalanceResponse.UnlockedBalance < atomicAmountToTip)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription("The faucet balance is pending and unable to spend. Please try again later.")
                .Build()).ConfigureAwait(false);
            
            return;
        }

        var userWalletAccount = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient).ConfigureAwait(false);
        var transferDestinations = new List<CommandRpcTransferSplit.TransferDestination>
        {
            new()
            {
                Address = userWalletAccount.TipBotWalletAddress,
                Amount = atomicAmountToTip
            }
        };

        var transferRequest = new CommandRpcTransferSplit.Request
        {
            AccountIndex = botWalletAccount.AccountIndex,
            Destinations = transferDestinations.ToArray(),
            Priority = 1,
            GetTransactionHex = true
        };

        var transferResult = await _walletRpcClient.TransferSplitAsync(transferRequest).ConfigureAwait(false);

        if (transferResult == null || transferResult.AmountList.Length == 0)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription("The transaction failed to process. This can be due to not enough unspent output or unable to cover the transaction fee.")
                .Build()).ConfigureAwait(false);
            
            return;
        }

        if (faucetHistory == null)
        {
            dbContext.FaucetHistories.Add(new FaucetHistory { UserId = Context.User.Id, DateTime = DateTimeOffset.Now });
        }
        else
        {
            faucetHistory.DateTime = DateTimeOffset.Now;
        }
        
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        await FollowupAsync(embed: new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithTitle("Faucet Result")
            .WithDescription($"You have rolled number {randomRewardWeight}.\nYou have won {DaemonUtility.FormatAtomicUnit(atomicAmountToTip, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} from the faucet.")
            .AddField("Transaction Hashes", $"{string.Join(", ", transferResult.TransactionHashList.Select(a => $"`{a}`"))}")
            .Build()).ConfigureAwait(false);
    }

    [SlashCommand("faucetpayout", "Check the current faucet payout table.")]
    public async Task FaucetPayoutCommand()
    {
        var outputTable = new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithTitle("Faucet Payout Table");

        decimal weightTotal = _discordOptions.Modules.Faucet.Amounts.Sum(amount => amount.Weight);
        var currentTotal = 0;

        foreach (var amount in _discordOptions.Modules.Faucet.Amounts)
        {
            outputTable.AddField($"{currentTotal}-{currentTotal + amount.Weight - 1} ({amount.Weight / weightTotal:P2})", $"{DaemonUtility.FormatAtomicUnit(amount.Amount, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}");
            currentTotal += amount.Weight;
        }

        await RespondAsync(embed: outputTable.Build()).ConfigureAwait(false);
    }
}