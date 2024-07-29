using System.Net;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options;

internal sealed class BlockchainOptions
{
    public string CoinName { get; set; } = string.Empty;
    
    public string CoinTicker { get; set; } = string.Empty;
    
    public ulong CoinUnit { get; set; }
    
    public ulong CoinMaxSupply { get; set; }
    
    public Rpc Rpc { get; set; } = new();
}

internal sealed class Rpc
{
    public TargetNetwork Daemon { get; set; } = new();
    
    public TargetNetwork Wallet { get; set; } = new();
}

internal sealed class TargetNetwork
{
    public string Host { get; set; } = IPAddress.Loopback.ToString();
    
    public int Port { get; set; }
}