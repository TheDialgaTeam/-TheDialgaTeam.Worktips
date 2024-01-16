using System.Globalization;
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

[Group("wallet", "Wallet Module")]
internal sealed class WalletModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly DiscordOptions _discordOptions;
    private readonly BlockchainOptions _blockchainOptions;
    private readonly IDbContextFactory<SqliteDatabaseContext> _dbContextFactory;
    private readonly WalletRpcClient _walletRpcClient;
    private readonly DaemonRpcClient _daemonRpcClient;

    public WalletModule(IOptions<DiscordOptions> discordOptions, IOptions<BlockchainOptions> blockchainOptions, IDbContextFactory<SqliteDatabaseContext> dbContextFactory, WalletRpcClient walletRpcClient, DaemonRpcClient daemonRpcClient)
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
    
    [SlashCommand("register", "Register/Update your wallet to your tip bot wallet.")]
    public async Task RegisterWalletCommand([Summary("walletAddress", "Your personal wallet address.")] string walletAddress)
    {
        await DeferAsync(true);
        
        if (!CheckWalletAddress(walletAddress))
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription($"This is not a valid {_blockchainOptions.CoinName} wallet address!")
                .Build(), ephemeral: true).ConfigureAwait(false);
            
            return;
        }
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var result = await dbContext.WalletAccounts.SingleOrDefaultAsync(account => account.UserId == Context.User.Id).ConfigureAwait(false);

        if (result == null)
        {
            var newWallet = await _walletRpcClient.CreateAccountAsync(new CommandRpcCreateAccount.Request { Label = Context.User.Id.ToString() }).ConfigureAwait(false);
            if (newWallet == null) throw new Exception("Could not create a new wallet.");

            var walletAccount = new WalletAccount
            {
                UserId = Context.User.Id,
                RegisteredWalletAddress = walletAddress,
                AccountIndex = newWallet.AccountIndex,
                TipBotWalletAddress = newWallet.Address
            };

            dbContext.WalletAccounts.Add(walletAccount);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Register Completed")
                .WithDescription($"Successfully registered your wallet!\nDeposit {_blockchainOptions.CoinTicker} to start tipping!")
                .AddField($"Your {_blockchainOptions.CoinTicker} Tip Bot Wallet Address:", $"`{newWallet.Address}`")
                .Build(), ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            result.RegisteredWalletAddress = walletAddress;
            await dbContext.SaveChangesAsync();

            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Update Completed")
                .WithDescription($"Successfully updated your wallet!")
                .Build(), ephemeral: true).ConfigureAwait(false);
        }
    }

    [SlashCommand("info", "Display your tip bot wallet information.")]
    public async Task WalletInfoCommand()
    {
        await DeferAsync(true).ConfigureAwait(false);
        
        var walletInfo = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient).ConfigureAwait(false);

        var balanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = walletInfo.AccountIndex }).ConfigureAwait(false);
        if (balanceResponse == null) throw new Exception();

        var heightResponse = await _walletRpcClient.GetHeightAsync().ConfigureAwait(false);
        if (heightResponse == null) throw new Exception();

        var daemonHeightResponse = await _daemonRpcClient.GetHeightAsync().ConfigureAwait(false);
        if (daemonHeightResponse == null) throw new Exception();

        await FollowupAsync(embed: new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithTitle("Your Tip Bot Wallet Information")
            .AddField("Deposit Address", $"`{walletInfo.TipBotWalletAddress}`")
            .AddField("Registered Address", $"`{walletInfo.RegisteredWalletAddress ?? string.Empty}`")
            .AddField("Available", $"{DaemonUtility.FormatAtomicUnit(balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
            .AddField($"Pending ({balanceResponse.BlocksToUnlock} blocks remaining)", $"{DaemonUtility.FormatAtomicUnit(balanceResponse.Balance - balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
            .AddField("Sync Status", $"{heightResponse.Height} / {daemonHeightResponse.Height}")
            .WithFooter($"Note: If you did not register your wallet, you will not be able to withdraw your {_blockchainOptions.CoinTicker}.")
            .Build(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("balance", "Check your tip bot wallet balance.")]
    public async Task WalletBalanceCommand()
    {
        await DeferAsync(true).ConfigureAwait(false);
        
        var walletInfo = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient).ConfigureAwait(false);

        var balanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = walletInfo.AccountIndex }).ConfigureAwait(false);
        if (balanceResponse == null) throw new Exception();

        var heightResponse = await _walletRpcClient.GetHeightAsync().ConfigureAwait(false);
        if (heightResponse == null) throw new Exception();

        var daemonHeightResponse = await _daemonRpcClient.GetHeightAsync().ConfigureAwait(false);
        if (daemonHeightResponse == null) throw new Exception();

        await FollowupAsync(embed: new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithTitle("Your Tip Bot Wallet Information")
            .AddField("Available", $"{DaemonUtility.FormatAtomicUnit(balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
            .AddField($"Pending ({balanceResponse.BlocksToUnlock} blocks remaining)", $"{DaemonUtility.FormatAtomicUnit(balanceResponse.Balance - balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
            .AddField("Sync Status", $"{heightResponse.Height} / {daemonHeightResponse.Height}")
            .WithFooter($"Note: If you did not register your wallet, you will not be able to withdraw your {_blockchainOptions.CoinTicker}.")
            .Build(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("botbalance", "Check the bot wallet balance.", true)]
    public async Task BotWalletBalanceCommand()
    {
        await DeferAsync().ConfigureAwait(false);
        
        var walletInfo = await GetOrCreateWalletAccountAsync(Context.Client.CurrentUser.Id, _dbContextFactory, _walletRpcClient).ConfigureAwait(false);

        var balanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = walletInfo.AccountIndex }).ConfigureAwait(false);
        if (balanceResponse == null) throw new Exception();

        var heightResponse = await _walletRpcClient.GetHeightAsync().ConfigureAwait(false);
        if (heightResponse == null) throw new Exception();

        var daemonHeightResponse = await _daemonRpcClient.GetHeightAsync().ConfigureAwait(false);
        if (daemonHeightResponse == null) throw new Exception();

        await FollowupAsync(embed: new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithTitle("Tip Bot Wallet Information")
            .AddField("Available", $"{DaemonUtility.FormatAtomicUnit(balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
            .AddField($"Pending ({balanceResponse.BlocksToUnlock} blocks remaining)", $"{DaemonUtility.FormatAtomicUnit(balanceResponse.Balance - balanceResponse.UnlockedBalance, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
            .AddField("Sync Status", $"{heightResponse.Height} / {daemonHeightResponse.Height}")
            .WithFooter($"Note: You can donate by tipping this bot by /tip <amount> {MentionUtils.MentionUser(Context.Client.CurrentUser.Id)}")
            .Build()).ConfigureAwait(false);
    }

    [SlashCommand("withdraw", "Withdraw your coins to the registered address.")]
    public async Task WalletWithdrawCommand([Summary("amount", "Amount to withdraw.")] decimal amount)
    {
        await DeferAsync(true).ConfigureAwait(false);
        
        var walletInfo = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient).ConfigureAwait(false);

        if (walletInfo.RegisteredWalletAddress == null)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription("You are required to register your wallet using `/wallet register` command!")
                .Build(), ephemeral: true).ConfigureAwait(false);

            return;
        }

        var atomicAmountToWithdraw = Convert.ToUInt64(Math.Floor(amount * _blockchainOptions.CoinUnit));

        if (atomicAmountToWithdraw < _discordOptions.Modules.Tip.WithdrawMinimumAmount)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription($"Minimum withdrawal amount is: {DaemonUtility.FormatAtomicUnit(_discordOptions.Modules.Tip.WithdrawMinimumAmount, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
                .Build(), ephemeral: true).ConfigureAwait(false);

            return;
        }

        var balanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = walletInfo.AccountIndex }).ConfigureAwait(false);
        if (balanceResponse == null) throw new Exception();

        if (atomicAmountToWithdraw > balanceResponse.UnlockedBalance)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription("Insufficient balance to withdraw this amount.")
                .Build(), ephemeral: true).ConfigureAwait(false);

            return;
        }

        var transferRequest = new CommandRpcTransferSplit.Request
        {
            AccountIndex = walletInfo.AccountIndex,
            Destinations =
            [
                new CommandRpcTransferSplit.TransferDestination
                {
                    Address = walletInfo.RegisteredWalletAddress,
                    Amount = atomicAmountToWithdraw
                }
            ],
            Priority = 1,
            GetTransactionHex = true
        };

        var transferResult = await _walletRpcClient.TransferSplitAsync(transferRequest).ConfigureAwait(false);

        if (transferResult == null || transferResult.AmountList.Length == 0)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Transfer Result")
                .WithDescription("Failed to withdraw this amount due to insufficient balance to cover the transaction fees.")
                .Build(), ephemeral: true).ConfigureAwait(false);
            
            return;
        }

        var response = new List<Embed>();

        var successEmbed = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithTitle("Transfer Result")
            .WithDescription($"You have withdrawn {DaemonUtility.FormatAtomicUnit(atomicAmountToWithdraw, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}");

        response.Add(successEmbed.Build());

        for (var i = 0; i < transferResult.TransactionHashList.Length; i++)
        {
            var txEmbed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle($"Transaction Paid ({i + 1}/{transferResult.TransactionHashList.Length})")
                .AddField("Amount", $"{DaemonUtility.FormatAtomicUnit(transferResult.AmountList[i], _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
                .AddField("Fee", $"{DaemonUtility.FormatAtomicUnit(transferResult.FeeList[i], _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
                .AddField("Transaction hash", $"`{transferResult.TransactionHashList[i]}`");

            response.Add(txEmbed.Build());
        }

        await FollowupAsync(embeds: response.ToArray(), ephemeral: true).ConfigureAwait(false);
    }
    
    [SlashCommand("tip", "Tip someone using your tip wallet.", true)]
    public async Task TipCommand([Summary("amount", "Amount to tip.")] decimal amount, [Summary("users", "Users to tip.")] string users)
    {
        await DeferAsync();
        
        var walletInfo = await GetOrCreateWalletAccountAsync(Context.User.Id, _dbContextFactory, _walletRpcClient).ConfigureAwait(false);

        var atomicAmountToTip = Convert.ToUInt64(Math.Floor(amount * _blockchainOptions.CoinUnit));

        if (atomicAmountToTip < _discordOptions.Modules.Tip.TipMinimumAmount)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription($"Minimum tip amount is: {DaemonUtility.FormatAtomicUnit(_discordOptions.Modules.Tip.TipMinimumAmount, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
                .Build()).ConfigureAwait(false);

            return;
        }

        var usersToTip = new List<IUser?>();
        var transferDestinations = new List<CommandRpcTransferSplit.TransferDestination>();
        var userTipped = new List<IUser>();
        
        foreach (var userText in users.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (MentionUtils.TryParseUser(userText, out var userId))
            {
                if (Context.Guild != null)
                {
                    usersToTip.Add(Context.Guild.GetUser(userId) ?? await Context.Channel.GetUserAsync(userId).ConfigureAwait(false));
                }
                else
                {
                    usersToTip.Add(await Context.Channel.GetUserAsync(userId).ConfigureAwait(false));
                }
            }
            else if (ulong.TryParse(userText, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            {
                if (Context.Guild != null)
                {
                    usersToTip.Add(Context.Guild.GetUser(id) ?? await Context.Channel.GetUserAsync(userId).ConfigureAwait(false));
                }
                else
                {
                    usersToTip.Add(await Context.Channel.GetUserAsync(id).ConfigureAwait(false));
                }
            }
        }
        
        foreach (var user in usersToTip)
        {
            if (user == null || user.Id == Context.User.Id || userTipped.Contains(user)) continue;

            var userWalletInfo = await GetOrCreateWalletAccountAsync(user.Id, _dbContextFactory, _walletRpcClient).ConfigureAwait(false);

            transferDestinations.Add(new CommandRpcTransferSplit.TransferDestination
            {
                Address = userWalletInfo.TipBotWalletAddress,
                Amount = atomicAmountToTip
            });

            userTipped.Add(user);
        }

        if (userTipped.Count == 0)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Transfer Result")
                .WithDescription("Failed to tip this amount due to no users to tip.")
                .Build()).ConfigureAwait(false);

            return;
        }
        
        var balanceResponse = await _walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = walletInfo.AccountIndex }).ConfigureAwait(false);
        if (balanceResponse == null) throw new Exception();

        if (atomicAmountToTip * Convert.ToUInt64(userTipped.Count) > balanceResponse.UnlockedBalance)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription("Insufficient balance to tip this amount.")
                .Build()).ConfigureAwait(false);

            return;
        }

        var transferRequest = new CommandRpcTransferSplit.Request
        {
            AccountIndex = walletInfo.AccountIndex,
            Destinations = transferDestinations.ToArray(),
            Priority = 1,
            GetTransactionHex = true
        };

        var transferResult = await _walletRpcClient.TransferSplitAsync(transferRequest).ConfigureAwait(false);

        if (transferResult == null || transferResult.AmountList.Length == 0)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Transfer Result")
                .WithDescription("Failed to tip this amount due to insufficient balance to cover the transaction fees.")
                .Build()).ConfigureAwait(false);
            
            return;
        }

        foreach (var user in userTipped)
        {
            if (user.IsBot) continue;

            try
            {
                var dmChannel = await user.CreateDMChannelAsync().ConfigureAwait(false);
                
                await dmChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle("Incoming Tip")
                    .WithDescription($"You got a tip of {DaemonUtility.FormatAtomicUnit(atomicAmountToTip, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} from {Context.User}\n:hash: Transaction hash: {string.Join(", ", transferResult.TransactionHashList.Select(a => $"`{a}`"))}")
                    .Build()).ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        }

        var response = new List<Embed>();
        
        var successEmbed = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithTitle("Transfer Result")
            .WithDescription($"You have tipped {DaemonUtility.FormatAtomicUnit(atomicAmountToTip, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker} to {userTipped.Count} users");

        response.Add(successEmbed.Build());

        for (var i = 0; i < transferResult.TransactionHashList.Length; i++)
        {
            var txEmbed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle($"Transaction Paid ({i + 1}/{transferResult.TransactionHashList.Length})")
                .AddField("Amount", $"{DaemonUtility.FormatAtomicUnit(transferResult.AmountList[i], _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
                .AddField("Fee", $"{DaemonUtility.FormatAtomicUnit(transferResult.FeeList[i], _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
                .AddField("Transaction hash", $"`{transferResult.TransactionHashList[i]}`");

            response.Add(txEmbed.Build());
        }

        await FollowupAsync(embeds: response.ToArray()).ConfigureAwait(false);
    }
}