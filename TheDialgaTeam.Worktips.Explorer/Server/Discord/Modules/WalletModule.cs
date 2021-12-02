using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Wallet;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;
using TheDialgaTeam.Worktips.Explorer.Server.Discord.Command;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Shared;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

[Name("Wallet")]
public class WalletModule : AbstractModule
{
    private readonly DiscordOptions _discordOptions;
    private readonly BlockchainOptions _blockchainOptions;
    private readonly IDbContextFactory<SqliteDatabaseContext> _dbContextFactory;
    private readonly WalletRpcClient _walletRpcClient;
    private readonly DaemonRpcClient _daemonRpcClient;

    public WalletModule(IHostApplicationLifetime hostApplicationLifetime, ILoggerTemplate<AbstractModule> logger, IOptions<DiscordOptions> discordOptions, IOptions<BlockchainOptions> blockchainOptions, IDbContextFactory<SqliteDatabaseContext> dbContextFactory, WalletRpcClient walletRpcClient, DaemonRpcClient daemonRpcClient) : base(hostApplicationLifetime, logger)
    {
        _discordOptions = discordOptions.Value;
        _blockchainOptions = blockchainOptions.Value;
        _dbContextFactory = dbContextFactory;
        _walletRpcClient = walletRpcClient;
        _daemonRpcClient = daemonRpcClient;
    }

    private static bool CheckWalletAddress(string address)
    {
        return address.StartsWith("Wtma", StringComparison.Ordinal) || address.StartsWith("Wtmi", StringComparison.Ordinal) || address.StartsWith("Wtms", StringComparison.Ordinal);
    }

    [Command("RegisterWallet")]
    [Alias("Register")]
    [Summary("Register/Update your wallet with the tip bot.")]
    [Example("RegisterWallet WtmaL4cVq7fVzT1VAtYpNUShZcRvjn1PubPVeKMMT7BM7hSFNA5aCSo6hiaGdzvB7GZfntpE4i5xZfAcQCdYhg3L9ynyQtgQEx")]
    public async Task RegisterWalletAsync([Summary("Wallet address.")] [Remainder] string walletAddress)
    {
        try
        {
            if (!CheckWalletAddress(walletAddress))
            {
                await ReplyAsync($"This is not a valid {_blockchainOptions.CoinName} address!");
                await AddReactionAsync("❌");
                return;
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var result = await dbContext.WalletAccounts.FindAsync(Context.User.Id);

            if (result == null)
            {
                var newWallet = await _walletRpcClient.CreateAccountAsync(new CommandRpcCreateAccount.Request { Label = Context.User.Id.ToString() });
                if (newWallet == null) throw new Exception("Could not create a new wallet.");

                var walletAccount = new WalletAccount
                {
                    UserId = Context.User.Id,
                    RegisteredWalletAddress = walletAddress,
                    AccountIndex = newWallet.AccountIndex,
                    TipWalletAddress = newWallet.Address
                };

                dbContext.WalletAccounts.Add(walletAccount);
                await dbContext.SaveChangesAsync();

                await ReplyToDirectMessageAsync($"Successfully registered your wallet!\nDeposit {_blockchainOptions.CoinTicker} to start tipping!\n\nYour {_blockchainOptions.CoinTicker} Tip Bot Address: `{newWallet.Address}`").ConfigureAwait(false);
                await AddReactionAsync("✅").ConfigureAwait(false);
            }
            else
            {
                result.RegisteredWalletAddress = walletAddress;
                await dbContext.SaveChangesAsync();

                await ReplyToDirectMessageAsync("Successfully updated your wallet!");
                await AddReactionAsync("✅").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("WalletInfo")]
    [Alias("Info")]
    [Summary("Display your wallet information.")]
    public async Task WalletInfoAsync()
    {
        try
        {
            var walletInfo = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient);

            await ReplyToDirectMessageAsync($":information_desk_person: ACCOUNT INFO\n:purse: Deposit Address: `{walletInfo.TipWalletAddress}`\n\n:purse: Registered Address: `{walletInfo.RegisteredWalletAddress ?? string.Empty}`\n\nNote: If you did not register your wallet, you will not be able to withdraw your {_blockchainOptions.CoinTicker}.");
            await AddReactionAsync("✅");
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("WalletBalance")]
    [Alias("Balance", "Bal")]
    [Summary("Check your wallet balance.")]
    public async Task WalletBalanceAsync()
    {
        try
        {
            var walletInfo = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient);

            var balanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = walletInfo.AccountIndex });
            if (balanceResponse == null) throw new Exception();

            var heightResponse = await _walletRpcClient.GetHeightAsync();
            if (heightResponse == null) throw new Exception();

            var daemonHeightResponse = await _daemonRpcClient.GetHeightAsync();
            if (daemonHeightResponse == null) throw new Exception();

            await ReplyToDirectMessageAsync($":moneybag: YOUR BALANCE\n:moneybag: Available: {DaemonUtility.FormatAtomicUnit(balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}\n:purse: Pending: {DaemonUtility.FormatAtomicUnit(balanceResponse.Balance - balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} ({balanceResponse.BlocksToUnlock} blocks remaining)\n:arrows_counterclockwise: Status: {heightResponse.Height} / {daemonHeightResponse.Height}");
            await AddReactionAsync("✅");
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("BotWalletBalance")]
    [Alias("BotBalance", "BotBal")]
    [Summary("Check the bot wallet balance.")]
    public async Task BotWalletBalanceAsync()
    {
        try
        {
            var walletInfo = await GetOrCreateWalletAccountAsync(Context.Client.CurrentUser.Id, _dbContextFactory, _walletRpcClient);

            var balanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = walletInfo.AccountIndex });
            if (balanceResponse == null) throw new Exception();

            var heightResponse = await _walletRpcClient.GetHeightAsync();
            if (heightResponse == null) throw new Exception();

            var daemonHeightResponse = await _daemonRpcClient.GetHeightAsync();
            if (daemonHeightResponse == null) throw new Exception();

            await ReplyAsync($":moneybag: TIP BOT BALANCE\n:moneybag: Available: {DaemonUtility.FormatAtomicUnit(balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}\n:purse: Pending: {DaemonUtility.FormatAtomicUnit(balanceResponse.Balance - balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} ({balanceResponse.BlocksToUnlock} blocks remaining)\n:arrows_counterclockwise: Status: {heightResponse.Height} / {daemonHeightResponse.Height}");
            await AddReactionAsync("✅");
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("WalletWithdraw")]
    [Alias("Withdraw")]
    [Summary("Withdraw your coins to the registered address.")]
    [Example("WalletWithdraw 1")]
    public async Task WalletWithdrawAsync([Summary("Amount to withdraw.")] decimal amount)
    {
        try
        {
            var walletInfo = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient);

            if (walletInfo.RegisteredWalletAddress == null)
            {
                await ReplyAsync("You are required to register your wallet using `RegisterWallet` command!\n\nFor more info, use `help RegisterWallet` for how to use the command.");
                await AddReactionAsync("❌");
                return;
            }

            var atomicAmountToWithdraw = Convert.ToUInt64(Math.Floor(amount * _blockchainOptions.CoinUnit));

            if (atomicAmountToWithdraw < _discordOptions.Modules.Tip.WithdrawMinimumAmount)
            {
                await ReplyAsync($":x: Minimum withdrawal amount is: {DaemonUtility.FormatAtomicUnit(_discordOptions.Modules.Tip.WithdrawMinimumAmount, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}");
                await AddReactionAsync("❌");
                return;
            }

            var balanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = walletInfo.AccountIndex });
            if (balanceResponse == null) throw new Exception();

            if (atomicAmountToWithdraw > balanceResponse.UnlockedBalance)
            {
                await ReplyAsync(":x: Insufficient balance to withdraw this amount.");
                await AddReactionAsync("❌");
                return;
            }

            var transferRequest = new CommandRpcTransferSplit.Request
            {
                AccountIndex = walletInfo.AccountIndex,
                Destinations = new[]
                {
                    new CommandRpcTransferSplit.TransferDestination
                    {
                        Address = walletInfo.RegisteredWalletAddress,
                        Amount = atomicAmountToWithdraw
                    }
                },
                Priority = 5,
                GetTransactionHex = true
            };

            var transferResult = await _walletRpcClient.TransferSplitAsync(transferRequest);

            if (transferResult == null || transferResult.AmountList.Length == 0)
            {
                var failEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle(":moneybag: TRANSFER RESULT")
                    .WithDescription("Failed to withdrawn this amount due to insufficient balance to cover the transaction fees.");

                await ReplyToDirectMessageAsync(embed: failEmbed.Build());
                await AddReactionAsync("❌");
                return;
            }

            var successEmbed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle(":moneybag: TRANSFER RESULT")
                .WithDescription($"You have withdrawn {DaemonUtility.FormatAtomicUnit(atomicAmountToWithdraw, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}");

            await ReplyToDirectMessageAsync(embed: successEmbed.Build());

            for (var i = 0; i < transferResult.TxHashList.Length; i++)
            {
                var txEmbed = new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithTitle($":moneybag: TRANSACTION PAID ({i + 1}/{transferResult.TxHashList.Length})")
                    .WithDescription($"Amount: {DaemonUtility.FormatAtomicUnit(transferResult.AmountList[i], _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}\nFee: {DaemonUtility.FormatAtomicUnit(transferResult.FeeList[i], _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}\nTransaction hash: `{transferResult.TxHashList[i]}`");

                await ReplyToDirectMessageAsync(embed: txEmbed.Build());
            }

            await AddReactionAsync("💰");
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("Tip")]
    [Summary("Tip someone using your tip wallet.")]
    [Example("Tip 1 @user\nTip 1 <userId>")]
    [RequireContext(ContextType.Guild)]
    public async Task TipAsync([Summary("Amount to tip.")] decimal amount, [Summary("Users to tip.")] params IUser[] users)
    {
        try
        {
            var walletInfo = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient);

            var atomicAmountToTip = Convert.ToUInt64(Math.Floor(amount * _blockchainOptions.CoinUnit));

            if (atomicAmountToTip < _discordOptions.Modules.Tip.TipMinimumAmount)
            {
                await ReplyAsync($":x: Minimum tip amount is: {DaemonUtility.FormatAtomicUnit(_discordOptions.Modules.Tip.TipMinimumAmount, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}");
                await AddReactionAsync("❌");
                return;
            }

            var balanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = walletInfo.AccountIndex });
            if (balanceResponse == null) throw new Exception();

            if (atomicAmountToTip * Convert.ToUInt64(users.Length) > balanceResponse.UnlockedBalance)
            {
                await ReplyAsync(":x: Insufficient balance to tip this amount.");
                await AddReactionAsync("❌");
                return;
            }

            var transferDestinations = new List<CommandRpcTransferSplit.TransferDestination>();
            var userTipped = new List<IUser>();

            foreach (var user in users)
            {
                if (user.Id == Context.Guild.Id || user.Id == Context.Channel.Id || user.Id == Context.User.Id || userTipped.Contains(user)) continue;

                var userWalletInfo = await GetOrCreateWalletAccountAsync(user.Id, _dbContextFactory, _walletRpcClient);

                transferDestinations.Add(new CommandRpcTransferSplit.TransferDestination
                {
                    Address = userWalletInfo.TipWalletAddress,
                    Amount = atomicAmountToTip
                });

                userTipped.Add(user);
            }

            if (userTipped.Count == 0)
            {
                var failEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle(":moneybag: TRANSFER RESULT")
                    .WithDescription("Failed to tip this amount due to no users to tip.");

                await ReplyToDirectMessageAsync(embed: failEmbed.Build());
                await AddReactionAsync("❌");
                return;
            }

            var transferRequest = new CommandRpcTransferSplit.Request
            {
                AccountIndex = walletInfo.AccountIndex,
                Destinations = transferDestinations.ToArray(),
                Priority = 1,
                GetTransactionHex = true
            };

            var transferResult = await _walletRpcClient.TransferSplitAsync(transferRequest);

            if (transferResult == null || transferResult.AmountList.Length == 0)
            {
                var failEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle(":moneybag: TRANSFER RESULT")
                    .WithDescription("Failed to tip this amount due to insufficient balance to cover the transaction fees.");

                await ReplyToDirectMessageAsync(embed: failEmbed.Build());
                await AddReactionAsync("❌");
                return;
            }

            foreach (var user in userTipped)
            {
                if (user.IsBot) continue;

                try
                {
                    var dmChannel = await user.GetOrCreateDMChannelAsync();

                    var notificationEmbed = new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle(":moneybag: INCOMING TIP")
                        .WithDescription($":moneybag: You got a tip of {DaemonUtility.FormatAtomicUnit(atomicAmountToTip, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} from {Context.User}\n:hash: Transaction hash: {string.Join(", ", transferResult.TxHashList.Select(a => $"`{a}`"))}");

                    await dmChannel.SendMessageAsync(embed: notificationEmbed.Build());
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            var successEmbed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle(":moneybag: TRANSFER RESULT")
                .WithDescription($"You have tipped {DaemonUtility.FormatAtomicUnit(atomicAmountToTip, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} to {userTipped.Count} users");

            await ReplyToDirectMessageAsync(embed: successEmbed.Build());

            for (var i = 0; i < transferResult.TxHashList.Length; i++)
            {
                var txEmbed = new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithTitle($":moneybag: TRANSACTION PAID ({i + 1}/{transferResult.TxHashList.Length})")
                    .WithDescription($"Amount: {DaemonUtility.FormatAtomicUnit(transferResult.AmountList[i], _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}\nFee: {DaemonUtility.FormatAtomicUnit(transferResult.FeeList[i], _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}\nTransaction hash: `{transferResult.TxHashList[i]}`");

                await ReplyToDirectMessageAsync(embed: txEmbed.Build());
            }

            await AddReactionAsync("💰");
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }
}