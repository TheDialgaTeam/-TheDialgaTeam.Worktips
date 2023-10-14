using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Daemon;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Shared;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

public sealed class DaemonModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly BlockchainOptions _blockchainOptions;
    private readonly IDbContextFactory<SqliteDatabaseContext> _dbContextFactory;
    private readonly DaemonRpcClient _daemonRpcClient;
    
    public DaemonModule(IOptions<BlockchainOptions> blockchainOptions, IDbContextFactory<SqliteDatabaseContext> dbContextFactory, DaemonRpcClient daemonRpcClient)
    {
        _blockchainOptions = blockchainOptions.Value;
        _dbContextFactory = dbContextFactory;
        _daemonRpcClient = daemonRpcClient;
    }

    [SlashCommand("network", "Get the network info.")]
    public async Task NetworkInfoCommand()
    {
        await DeferAsync().ConfigureAwait(false);
        
        var infoResponse = await _daemonRpcClient.GetInfoAsync().ConfigureAwait(false);
        if (infoResponse == null) return;

        var blockResponse = await _daemonRpcClient.GetBlockAsync(new CommandRpcGetBlock.Request {Height = infoResponse.Height - 1}).ConfigureAwait(false);
        if (blockResponse == null) return;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var daemonSyncHistory = await dbContext.DaemonSyncHistory.SingleAsync().ConfigureAwait(false);

        await FollowupAsync(embed: new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithTitle("Blockchain Information")
            .AddField("Height", $"{infoResponse.Height:N0}")
            .AddField("Difficulty", $"{infoResponse.Difficulty:N0}")
            .AddField("Network Hash Rate", $"{DaemonUtility.FormatHashrate(Convert.ToDouble(infoResponse.Difficulty) / Convert.ToDouble(infoResponse.Target))}")
            .AddField("Block Reward", $"{DaemonUtility.FormatAtomicUnit(blockResponse.BlockHeader.Reward, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
            .AddField("Miner Reward", $"{DaemonUtility.FormatAtomicUnit(blockResponse.BlockHeader.MinerReward, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}", true)
            .AddField("Service Node Reward", $"{DaemonUtility.FormatAtomicUnit(blockResponse.BlockHeader.Reward - blockResponse.BlockHeader.MinerReward, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}", true)
            .AddField("Total Transactions", $"{infoResponse.TransactionCount:N0}")
            .AddField($"Circulating Supply ({daemonSyncHistory.BlockCount})", $"{DaemonUtility.FormatAtomicUnit(daemonSyncHistory.TotalCirculation, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}", true)
            .AddField("Total Supply", $"{DaemonUtility.FormatAtomicUnit(_blockchainOptions.CoinMaxSupply, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}", true)
            .AddField("Current Emission", $"{Convert.ToDouble(daemonSyncHistory.TotalCirculation) / Convert.ToDouble(_blockchainOptions.CoinMaxSupply):P2}")
            .WithUrl("https://worktips.gatewayroleplay.org")
            .Build()).ConfigureAwait(false);
    }
}