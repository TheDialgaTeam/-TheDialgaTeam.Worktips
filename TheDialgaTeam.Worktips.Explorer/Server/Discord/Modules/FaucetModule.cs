using System.Security.Cryptography;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Wallet;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Shared;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

[Name("Faucet")]
public class FaucetModule : AbstractModule
{
    private readonly DiscordOptions _discordOptions;
    private readonly BlockchainOptions _blockchainOptions;
    private readonly IDbContextFactory<SqliteDatabaseContext> _dbContextFactory;
    private readonly WalletRpcClient _walletRpcClient;

    public FaucetModule(IHostApplicationLifetime hostApplicationLifetime, ILoggerTemplate<AbstractModule> logger, IOptions<DiscordOptions> discordOptions, IOptions<BlockchainOptions> blockchainOptions, IDbContextFactory<SqliteDatabaseContext> dbContextFactory, WalletRpcClient walletRpcClient) : base(hostApplicationLifetime, logger)
    {
        _discordOptions = discordOptions.Value;
        _blockchainOptions = blockchainOptions.Value;
        _dbContextFactory = dbContextFactory;
        _walletRpcClient = walletRpcClient;
    }

    [Command("Faucet")]
    [Alias("Drizzle", "gimmelove")]
    [Summary("Get a small tip from the faucet.")]
    public async Task FaucetAsync()
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var faucetHistory = await dbContext.FaucetHistories.FindAsync(Context.User.Id);

            if (faucetHistory != null && DateTimeOffset.Now - faucetHistory.DateTime < TimeSpan.FromHours(1))
            {
                await ReplyAsync("You have to wait for the next hour before you can receive tips again.");
                await AddReactionAsync("❌");
                return;
            }

            var botWalletAccount = await GetOrCreateWalletAccountAsync(Context.Client.CurrentUser.Id, _dbContextFactory, _walletRpcClient);
            var botBalanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = botWalletAccount.AccountIndex });
            if (botBalanceResponse == null) throw new Exception();

            if (botBalanceResponse.Balance <= _discordOptions.Modules.Faucet.Amounts.Max(amount => amount.Amount))
            {
                await ReplyAsync($"The faucet requires a minimum of {DaemonUtility.FormatAtomicUnit(_discordOptions.Modules.Faucet.Amounts.Max(amount => amount.Amount), _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} in the bot balance. Please consider donating to keep this faucet running :)");
                await AddReactionAsync("❌");
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
                await ReplyAsync("The faucet balance is pending and unable to spend. Please try again later.");
                await AddReactionAsync("❌");
                return;
            }

            var userWalletAccount = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient);
            var transferDestinations = new List<CommandRpcTransferSplit.TransferDestination>
            {
                new()
                {
                    Address = userWalletAccount.TipWalletAddress,
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

            var transferResult = await _walletRpcClient.TransferSplitAsync(transferRequest);

            if (transferResult == null || transferResult.AmountList.Length == 0)
            {
                await ReplyAsync("The transaction failed to process. This can be due to not enough unspent output or unable to cover the transaction fee.");
                await AddReactionAsync("❌");
                return;
            }

            if (faucetHistory == null)
            {
                dbContext.FaucetHistories.Add(new FaucetHistory { UserId = Context.User.Id, DateTime = DateTimeOffset.Now });
                await dbContext.SaveChangesAsync();
            }
            else
            {
                faucetHistory.DateTime = DateTimeOffset.Now;
                await dbContext.SaveChangesAsync();
            }

            try
            {
                var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

                var notificationEmbed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle(":moneybag: INCOMING TIP")
                    .WithDescription($":moneybag: You got a tip of {DaemonUtility.FormatAtomicUnit(atomicAmountToTip, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} from the faucet.\n:hash: Transaction hash: {string.Join(", ", transferResult.TransactionHashList.Select(a => $"`{a}`"))}");

                await dmChannel.SendMessageAsync(embed: notificationEmbed.Build());
            }
            catch (Exception)
            {
                // ignored
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle(":moneybag: Faucet Result")
                .WithDescription($"You have rolled number {randomRewardWeight}.\nYou have won {DaemonUtility.FormatAtomicUnit(atomicAmountToTip, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} from the faucet.")
                .Build()
            );

            await AddReactionAsync("💰");
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }
}