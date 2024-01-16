namespace TheDialgaTeam.Worktips.Explorer.Server.Options.Blockchain;

internal sealed class Rpc
{
    public TargetNetwork Daemon { get; set; } = new();
    
    public TargetNetwork Wallet { get; set; } = new();
}