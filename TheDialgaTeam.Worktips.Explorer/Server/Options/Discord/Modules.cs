using TheDialgaTeam.Worktips.Explorer.Server.Options.Discord.Faucet;
using TheDialgaTeam.Worktips.Explorer.Server.Options.Discord.Tip;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options.Discord;

internal sealed class Modules
{
    public TipModule Tip { get; set; } = new();
    
    public FaucetModule Faucet { get; set; } = new();
}