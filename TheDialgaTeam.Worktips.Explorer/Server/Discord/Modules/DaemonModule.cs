using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Daemon;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Shared;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

[Name("Daemon")]
public class DaemonModule : AbstractModule
{
    private readonly BlockchainOptions _blockchainOptions;
    private readonly DaemonRpcClient _daemonRpcClient;
    private readonly IDbContextFactory<SqliteDatabaseContext> _dbContextFactory;

    public DaemonModule(IHostApplicationLifetime hostApplicationLifetime, ILoggerTemplate<AbstractModule> logger, IOptions<BlockchainOptions> blockchainOptions, DaemonRpcClient daemonRpcClient, IDbContextFactory<SqliteDatabaseContext> dbContextFactory) : base(hostApplicationLifetime, logger)
    {
        _blockchainOptions = blockchainOptions.Value;
        _daemonRpcClient = daemonRpcClient;
        _dbContextFactory = dbContextFactory;
    }

    [Command("NetworkInfo")]
    [Alias("Network")]
    [Summary("Get the network info.")]
    public async Task NetworkInfoAsync()
    {
        try
        {
            var infoResponse = await _daemonRpcClient.GetInfoAsync();
            if (infoResponse == null) return;

            var blockResponse = await _daemonRpcClient.GetBlockHeaderByHeightAsync(new CommandRpcGetBlockHeaderByHeight.Request { Height = infoResponse.Height - 1 });
            if (blockResponse == null) return;

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var daemonSyncHistory = dbContext.DaemonSyncHistory.First();

            await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle("Blockchain Information")
                .WithColor(Color.DarkOrange)
                .AddField("Height", $"{infoResponse.Height:N0}")
                .AddField("Difficulty", $"{infoResponse.Difficulty:N0}")
                .AddField("Network Hash Rate", $"{DaemonUtility.FormatHashrate(Convert.ToDouble(infoResponse.Difficulty) / Convert.ToDouble(infoResponse.Target))}")
                .AddField("Block Reward", $"{DaemonUtility.FormatAtomicUnit(blockResponse.BlockHeader.Reward, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
                .AddField("Total Transactions", $"{infoResponse.TransactionCount:N0}")
                .AddField($"Circulating Supply ({daemonSyncHistory.BlockCount})", $"{DaemonUtility.FormatAtomicUnit(daemonSyncHistory.TotalCirculation, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
                .AddField("Total Supply", $"{DaemonUtility.FormatAtomicUnit(_blockchainOptions.CoinMaxSupply, _blockchainOptions.CoinUnit)} {_blockchainOptions.CoinTicker}")
                .AddField("Current Emission", $"{Convert.ToDouble(daemonSyncHistory.TotalCirculation) / Convert.ToDouble(_blockchainOptions.CoinMaxSupply):P2}")
                .WithUrl("https://worktips.gatewayroleplay.org")
                .Build());
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("Hashrate")]
    [Alias("Hash")]
    [Summary("Get the network hashrate.")]
    public async Task HashrateAsync()
    {
        try
        {
            var response = await _daemonRpcClient.GetInfoAsync();
            if (response == null) return;

            await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle("Blockchain Information")
                .WithColor(Color.DarkOrange)
                .AddField("Network Hash Rate", $"{DaemonUtility.FormatHashrate(Convert.ToDouble(response.Difficulty) / Convert.ToDouble(response.Target))}")
                .WithUrl("https://worktips.gatewayroleplay.org")
                .Build());
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("Difficulty")]
    [Alias("Diff")]
    [Summary("Get the network difficulty.")]
    public async Task DifficultyAsync()
    {
        try
        {
            var response = await _daemonRpcClient.GetInfoAsync();
            if (response == null) return;

            await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle("Blockchain Information")
                .WithColor(Color.DarkOrange)
                .AddField("Difficulty", $"{response.Difficulty:N0}")
                .WithUrl("https://worktips.gatewayroleplay.org")
                .Build());

            await ReplyAsync($"The current difficulty is **{response.Difficulty}**");
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("Height")]
    [Summary("Get the current length of longest chain known to daemon.")]
    public async Task HeightAsync()
    {
        try
        {
            var response = await _daemonRpcClient.GetInfoAsync();
            if (response == null) return;

            await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle("Blockchain Information")
                .WithColor(Color.DarkOrange)
                .AddField("Height", $"{response.Height:N0}")
                .WithUrl("https://worktips.gatewayroleplay.org")
                .Build());
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }
}