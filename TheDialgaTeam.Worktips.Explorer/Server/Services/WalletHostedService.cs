using TheDialgaTeam.Cryptonote.Rpc.Worktips;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

internal sealed class WalletHostedService(ILogger<WalletHostedService> logger, WalletRpcClient walletRpcClient) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken).ConfigureAwait(false);
            await walletRpcClient.StoreAsync(stoppingToken).ConfigureAwait(false);
            Logger.PrintWalletSaved(logger);
        }
    }
}