using JetBrains.Annotations;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options.Blockchain;

public sealed class TargetNetwork
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string Host { get; init; } = "127.0.0.1";

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int Port { get; init; }
}