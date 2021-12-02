using Discord.Commands;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Worktips.Explorer.Shared;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

[Name("Daemon")]
public class DaemonModule : AbstractModule
{
    private readonly DaemonRpcClient _daemonRpcClient;

    public DaemonModule(IHostApplicationLifetime hostApplicationLifetime, ILoggerTemplate<AbstractModule> logger, DaemonRpcClient daemonRpcClient) : base(hostApplicationLifetime, logger)
    {
        _daemonRpcClient = daemonRpcClient;
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

            var hashrate = Convert.ToDecimal(response.Difficulty) / Convert.ToDecimal(response.Target);

            await ReplyAsync($"The current network hashrate is **{DaemonUtility.FormatHashrate((double) hashrate)}**");
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

            await ReplyAsync($"The current height is **{response.Height}**");
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }
}