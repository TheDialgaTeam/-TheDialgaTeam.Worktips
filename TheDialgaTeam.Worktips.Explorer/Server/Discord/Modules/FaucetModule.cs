using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Options;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

[Name("Faucet")]
public class FaucetModule : AbstractModule
{
    private readonly DiscordOptions _discordOptions;
    private readonly BlockchainOptions _blockchainOptions;
    private readonly IDbContextFactory<SqliteDatabaseContext> _dbContextFactory;
    private readonly WalletRpcClient _walletRpcClient;
    private readonly DaemonRpcClient _daemonRpcClient;

    public FaucetModule(IHostApplicationLifetime hostApplicationLifetime, ILoggerTemplate<AbstractModule> logger, IOptions<DiscordOptions> discordOptions, IOptions<BlockchainOptions> blockchainOptions, IDbContextFactory<SqliteDatabaseContext> dbContextFactory, WalletRpcClient walletRpcClient, DaemonRpcClient daemonRpcClient) : base(hostApplicationLifetime, logger)
    {
        _discordOptions = discordOptions.Value;
        _blockchainOptions = blockchainOptions.Value;
        _dbContextFactory = dbContextFactory;
        _walletRpcClient = walletRpcClient;
        _daemonRpcClient = daemonRpcClient;
    }

    [Command("Faucet")]
    [Alias("Drizzle", "gimmelove")]
    [Summary("Get a small tip from the faucet.")]
    public async Task FaucetAsync()
    {
        try
        {
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }
}