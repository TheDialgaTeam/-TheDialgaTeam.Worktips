using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Daemon;
using TheDialgaTeam.Worktips.Explorer.Server.Database;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

public sealed class DaemonHostedService : BackgroundService
{
    private readonly ILogger<DaemonHostedService> _logger;
    private readonly IDbContextFactory<SqliteDatabaseContext> _contextFactory;
    private readonly DaemonRpcClient _daemonRpcClient;

    public DaemonHostedService(ILogger<DaemonHostedService> logger, IDbContextFactory<SqliteDatabaseContext> contextFactory, DaemonRpcClient daemonRpcClient)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _daemonRpcClient = daemonRpcClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken).ConfigureAwait(false);
        await context.Database.MigrateAsync(stoppingToken).ConfigureAwait(false);

        var daemonSyncHistory = await context.DaemonSyncHistory.SingleAsync(stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var maxHeightResponse = await _daemonRpcClient.GetHeightAsync(stoppingToken).ConfigureAwait(false);
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
                    var blockResponse = await _daemonRpcClient.GetBlockHeadersRangeAsync(new CommandRpcGetBlockHeadersRange.Request { StartHeight = daemonSyncHistory.BlockCount + 1, EndHeight = daemonSyncHistory.BlockCount + 1000 <= maxHeight ? daemonSyncHistory.BlockCount + 1000 : maxHeight, FillPowHash = false }, stoppingToken).ConfigureAwait(false);
                    if (blockResponse == null) break;

                    foreach (var blockResponseHeader in blockResponse.Headers)
                    {
                        daemonSyncHistory.BlockCount = blockResponseHeader.Height;
                        daemonSyncHistory.TotalCirculation += blockResponseHeader.Reward;
                    }

                    await context.SaveChangesAsync(stoppingToken).ConfigureAwait(false);

                    Logger.PrintDaemonSynchronizeStatus(_logger, daemonSyncHistory.BlockCount, maxHeight);
                } while (daemonSyncHistory.BlockCount != maxHeight);
            }
            catch (HttpRequestException)
            {
            }
        }
    }
}