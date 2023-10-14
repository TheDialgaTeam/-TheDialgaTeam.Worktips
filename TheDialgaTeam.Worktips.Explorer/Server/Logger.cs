using static TheDialgaTeam.Core.Logging.Microsoft.AnsiEscapeCodeConstants;

namespace TheDialgaTeam.Worktips.Explorer.Server;

public static partial class Logger
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = $"{GreenForegroundColor}Synchronized {{BlockCount}}/{{MaxHeight}}{Reset}")]
    public static partial void PrintDaemonSynchronizeStatus(ILogger logger, ulong blockCount, ulong maxHeight);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = $"{GreenForegroundColor}Wallet Saved.{Reset}")]
    public static partial void PrintWalletSaved(ILogger logger);
}