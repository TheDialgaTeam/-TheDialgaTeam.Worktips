using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services
{
    public class WalletHostedService : IHostedService, IDisposable
    {
        private readonly WalletRpcClient _walletRpcClient;
        private readonly ILoggerTemplate<WalletHostedService> _logger;

        private Timer? _timer;

        public WalletHostedService(WalletRpcClient walletRpcClient, ILoggerTemplate<WalletHostedService> logger)
        {
            _walletRpcClient = walletRpcClient;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(_ =>
            {
                try
                {
                    _walletRpcClient.StoreAsync(cancellationToken);
                    _logger.LogInformation($"{AnsiEscapeCodeConstants.GreenForegroundColor}Wallet saved.{AnsiEscapeCodeConstants.Reset}", true);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error:", true);
                }
            });

            _timer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(0, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
