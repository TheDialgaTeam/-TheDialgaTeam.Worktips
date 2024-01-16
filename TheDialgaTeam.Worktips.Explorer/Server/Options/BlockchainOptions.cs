using TheDialgaTeam.Worktips.Explorer.Server.Options.Blockchain;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options;

internal sealed class BlockchainOptions
{
    public string CoinName { get; set; } = string.Empty;
    
    public string CoinTicker { get; set; } = string.Empty;
    
    public ulong CoinUnit { get; set; }
    
    public ulong CoinMaxSupply { get; set; }
    
    public Rpc Rpc { get; set; } = new();
}