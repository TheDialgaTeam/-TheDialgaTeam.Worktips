using TheDialgaTeam.Worktips.Explorer.Server.Options.Discord.Faucet;
using TheDialgaTeam.Worktips.Explorer.Server.Options.Discord.Tip;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options.Discord;

public class Modules
{
    public TipModule Tip { get; set; } = null!;

    public FaucetModule Faucet { get; set; } = null!;
}