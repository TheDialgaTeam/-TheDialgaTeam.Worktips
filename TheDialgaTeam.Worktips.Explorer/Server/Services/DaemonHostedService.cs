using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Daemon;
using TheDialgaTeam.Worktips.Explorer.Server.Database;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

public class DaemonHostedService : IHostedService
{
    private readonly ILoggerTemplate<DaemonHostedService> _loggerTemplate;
    private readonly DaemonRpcClient _daemonRpcClient;
    private readonly IDbContextFactory<SqliteDatabaseContext> _context;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public DaemonHostedService(ILoggerTemplate<DaemonHostedService> loggerTemplate, DaemonRpcClient daemonRpcClient, IDbContextFactory<SqliteDatabaseContext> context, IHostApplicationLifetime hostApplicationLifetime)
    {
        _loggerTemplate = loggerTemplate;
        _daemonRpcClient = daemonRpcClient;
        _context = context;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(async () =>
        {
            var applicationStopping = _hostApplicationLifetime.ApplicationStopping;

            while (!applicationStopping.IsCancellationRequested)
            {
                try
                {
                    await using var context = await _context.CreateDbContextAsync(applicationStopping);
                    var daemonSyncHistory = await context.DaemonSyncHistory.FirstOrDefaultAsync(applicationStopping);
                    if (daemonSyncHistory == null) return;

                    var maxHeightResponse = await _daemonRpcClient.GetHeightAsync(applicationStopping);
                    if (maxHeightResponse == null) continue;

                    var maxHeight = maxHeightResponse.Height - 1;

                    if (daemonSyncHistory.BlockCount == maxHeight)
                    {
                        await Task.Delay(1000, applicationStopping);
                        continue;
                    }

                    do
                    {
                        // Batch query
                        var blockResponse = await _daemonRpcClient.GetBlockHeadersRangeAsync(new CommandRpcGetBlockHeadersRange.Request { StartHeight = daemonSyncHistory.BlockCount + 1, EndHeight = daemonSyncHistory.BlockCount + 1000 <= maxHeight ? daemonSyncHistory.BlockCount + 1000 : maxHeight, FillPowHash = false }, applicationStopping);
                        if (blockResponse == null) break;

                        foreach (var blockResponseHeader in blockResponse.Headers)
                        {
                            daemonSyncHistory.BlockCount = blockResponseHeader.Height;
                            daemonSyncHistory.TotalCirculation += blockResponseHeader.Reward;
                        }

                        await context.SaveChangesAsync(applicationStopping);

                        _loggerTemplate.LogInformation($"{AnsiEscapeCodeConstants.DarkGreenForegroundColor}Synchronized {daemonSyncHistory.BlockCount}/{maxHeight}{AnsiEscapeCodeConstants.Reset}", true);
                    } while (daemonSyncHistory.BlockCount != maxHeight);
                }
                catch (Exception exception) when (exception is not OperationCanceledException && exception is not TaskCanceledException)
                {
                    _loggerTemplate.LogError(exception, "Error:", true);
                    await Task.Delay(1000, applicationStopping);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }, TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}