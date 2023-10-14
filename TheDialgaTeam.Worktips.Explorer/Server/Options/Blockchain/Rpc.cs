using JetBrains.Annotations;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options.Blockchain;

public sealed class Rpc
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public TargetNetwork Daemon { get; init; } = null!;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public TargetNetwork Wallet { get; init; } = null!;
}