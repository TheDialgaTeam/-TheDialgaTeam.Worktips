using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Daemon;
using TheDialgaTeam.Worktips.Explorer.Server.Database;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

internal sealed class DaemonHostedService(
    ILogger<DaemonHostedService> logger, 
    IDbContextFactory<SqliteDatabaseContext> contextFactory, 
    DaemonRpcClient daemonRpcClient) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(stoppingToken).ConfigureAwait(false);
        await context.Database.MigrateAsync(stoppingToken).ConfigureAwait(false);

        var daemonSyncHistory = await context.DaemonSyncHistory.SingleAsync(stoppingToken).ConfigureAwait(false);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var maxHeightResponse = await daemonRpcClient.GetHeightAsync(stoppingToken).ConfigureAwait(false);
                if (maxHeightResponse == null) continue;

                var maxHeight = maxHeightResponse.Height - 1;

                if (daemonSyncHistory.BlockCount == maxHeight)
                {
                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                do
                {
                    // Batch query
                    var blockResponse = await daemonRpcClient.GetBlockHeadersRangeAsync(new CommandRpcGetBlockHeadersRange.Request { StartHeight = daemonSyncHistory.BlockCount + 1, EndHeight = daemonSyncHistory.BlockCount + 1000 <= maxHeight ? daemonSyncHistory.BlockCount + 1000 : maxHeight, FillPowHash = false }, stoppingToken).ConfigureAwait(false);
                    if (blockResponse == null) break;

                    foreach (var blockResponseHeader in blockResponse.Headers)
                    {
                        daemonSyncHistory.BlockCount = blockResponseHeader.Height;
                        daemonSyncHistory.TotalCirculation += blockResponseHeader.Reward;
                    }

                    await context.SaveChangesAsync(stoppingToken).ConfigureAwait(false);

                    Logger.PrintDaemonSynchronizeStatus(logger, daemonSyncHistory.BlockCount, maxHeight);
                } while (daemonSyncHistory.BlockCount != maxHeight);
            }
            catch (HttpRequestException)
            {
            }
        }
    }
}