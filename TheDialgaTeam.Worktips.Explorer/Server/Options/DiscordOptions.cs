using JetBrains.Annotations;
using TheDialgaTeam.Worktips.Explorer.Server.Options.Discord;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options;

public sealed class DiscordOptions
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string BotToken { get; init; } = null!;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public Modules Modules { get; init; } = null!;
}