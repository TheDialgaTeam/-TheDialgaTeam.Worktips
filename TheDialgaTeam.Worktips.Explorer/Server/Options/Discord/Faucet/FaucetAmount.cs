using JetBrains.Annotations;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options.Discord.Faucet;

public sealed class FaucetAmount
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public ulong Amount { get; init; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int Weight { get; init; }
}