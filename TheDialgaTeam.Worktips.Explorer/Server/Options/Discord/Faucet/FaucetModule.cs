using JetBrains.Annotations;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options.Discord.Faucet;

public sealed class FaucetModule
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public FaucetAmount[] Amounts { get; init; } = Array.Empty<FaucetAmount>();
}