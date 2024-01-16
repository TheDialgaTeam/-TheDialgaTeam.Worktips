using static TheDialgaTeam.Serilog.Formatting.AnsiEscapeCodeConstants;

namespace TheDialgaTeam.Worktips.Explorer.Server;

internal static partial class Logger
{
    [LoggerMessage(Level = LogLevel.Information, Message = $"{GreenForegroundColor}Synchronized {{BlockCount}}/{{MaxHeight}}{Reset}")]
    public static partial void PrintDaemonSynchronizeStatus(ILogger logger, ulong blockCount, ulong maxHeight);

    [LoggerMessage(Level = LogLevel.Information, Message = $"{GreenForegroundColor}Wallet Saved.{Reset}")]
    public static partial void PrintWalletSaved(ILogger logger);
}