using System.Security.Cryptography;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Wallet;
using TheDialgaTeam.Worktips.Explorer.Server.Database.Repositories;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Shared.Utilities;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

internal sealed class FaucetModule(
    IOptions<DiscordOptions> discordOptions,
    IOptions<BlockchainOptions> blockchainOptions,
    WalletAccountRepository walletAccountRepository,
    FaucetHistoryRepository faucetHistoryRepository,
    WalletRpcClient walletRpcClient) :
    InteractionModuleBase<InteractionContext>
{
    private readonly DiscordOptions _discordOptions = discordOptions.Value;
    private readonly BlockchainOptions _blockchainOptions = blockchainOptions.Value;

    [SlashCommand("faucet", "Get a small tip from the faucet.", true)]
    public async Task FaucetCommand()
    {
        await DeferAsync().ConfigureAwait(false);

        if (!faucetHistoryRepository.IsFaucetClaimable(Context.User.Id, out var duration))
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription($"You have to wait for {duration.TotalMinutes:N0} minutes before you can receive tips again.")
                .Build()).ConfigureAwait(false);

            return;
        }

        var botWalletAccount = await walletAccountRepository.GetOrCreateWalletAccountAsync(Context.Client.CurrentUser.Id).ConfigureAwait(false);
        var botBalanceResponse = await walletRpcClient.GetBalanceAsync(new CommandRpcGetBalance.Request { AccountIndex = botWalletAccount.AccountIndex }).ConfigureAwait(false);
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

        var userWalletAccount = await walletAccountRepository.GetOrCreateWalletAccountAsync(Context.User.Id).ConfigureAwait(false);
        var transferDestinations = new List<CommandRpcTransferSplit.TransferDestination>
        {
            new()
            {
                Address = userWalletAccount.SentToRegisteredWalletDirectly ? userWalletAccount.RegisteredWalletAddress ?? userWalletAccount.TipBotWalletAddress : userWalletAccount.TipBotWalletAddress,
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

        var transferResult = await walletRpcClient.TransferSplitAsync(transferRequest).ConfigureAwait(false);

        if (transferResult == null || transferResult.AmountList.Length == 0)
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription("The transaction failed to process. This can be due to not enough unspent output or unable to cover the transaction fee.")
                .Build()).ConfigureAwait(false);

            return;
        }

        await faucetHistoryRepository.SetFaucetClaimedAsync(Context.User.Id).ConfigureAwait(false);

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