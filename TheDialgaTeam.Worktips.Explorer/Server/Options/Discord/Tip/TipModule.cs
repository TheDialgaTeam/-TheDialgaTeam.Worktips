using JetBrains.Annotations;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options.Discord.Tip;

public sealed class TipModule
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public ulong TipMinimumAmount { get; init; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public ulong WithdrawMinimumAmount { get; init; }
}