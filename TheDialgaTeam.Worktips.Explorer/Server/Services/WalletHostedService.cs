using TheDialgaTeam.Cryptonote.Rpc.Worktips;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

internal sealed class WalletHostedService(ILogger<WalletHostedService> logger, WalletRpcClient walletRpcClient) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        
        while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            await walletRpcClient.StoreAsync(stoppingToken).ConfigureAwait(false);
            Logger.PrintWalletSaved(logger);
        }
    }
}