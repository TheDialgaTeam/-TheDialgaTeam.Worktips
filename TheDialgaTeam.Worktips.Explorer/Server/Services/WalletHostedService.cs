using TheDialgaTeam.Cryptonote.Rpc.Worktips;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

public sealed class WalletHostedService : BackgroundService
{
    private readonly ILogger<WalletHostedService> _logger;
    private readonly WalletRpcClient _walletRpcClient;

    public WalletHostedService(ILogger<WalletHostedService> logger, WalletRpcClient walletRpcClient)
    {
        _logger = logger;
        _walletRpcClient = walletRpcClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken).ConfigureAwait(false);
            await _walletRpcClient.StoreAsync(stoppingToken).ConfigureAwait(false);
            Logger.PrintWalletSaved(_logger);
        }
    }
}