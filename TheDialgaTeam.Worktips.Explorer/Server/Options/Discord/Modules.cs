using JetBrains.Annotations;
using TheDialgaTeam.Worktips.Explorer.Server.Options.Discord.Faucet;
using TheDialgaTeam.Worktips.Explorer.Server.Options.Discord.Tip;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options.Discord;

public sealed class Modules
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public TipModule Tip { get; init; } = null!;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public FaucetModule Faucet { get; init; } = null!;
}