using TheDialgaTeam.Worktips.Explorer.Server.Options.Blockchain;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options;

public class BlockchainOptions
{
    public string CoinName { get; set; } = null!;

    public string CoinTicker { get; set; } = null!;

    public ulong CoinUnit { get; set; }

    public ulong CoinMaxSupply { get; set; }

    public Rpc Rpc { get; set; } = null!;
}