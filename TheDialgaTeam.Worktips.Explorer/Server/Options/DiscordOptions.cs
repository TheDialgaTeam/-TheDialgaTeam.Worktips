using TheDialgaTeam.Worktips.Explorer.Server.Options.Discord;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options;

public class DiscordOptions
{
    public string BotToken { get; set; } = null!;

    public string BotPrefix { get; set; } = null!;

    public Modules Modules { get; set; } = null!;
}