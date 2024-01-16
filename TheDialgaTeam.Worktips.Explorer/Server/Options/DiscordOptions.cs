using TheDialgaTeam.Worktips.Explorer.Server.Options.Discord;

namespace TheDialgaTeam.Worktips.Explorer.Server.Options;

internal sealed class DiscordOptions
{
    public string BotToken { get; set; } = string.Empty;
    
    public Modules Modules { get; set; } = new();
}