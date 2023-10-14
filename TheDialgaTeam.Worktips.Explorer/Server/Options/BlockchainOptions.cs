using JetBrains.Annotations;
using TheDialgaTeam.Worktips.Explorer.Server.Options.Blockchain;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options;

public sealed class BlockchainOptions
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string CoinName { get; init; } = null!;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string CoinTicker { get; init; } = null!;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public ulong CoinUnit { get; init; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public ulong CoinMaxSupply { get; init; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public Rpc Rpc { get; init; } = null!;
}