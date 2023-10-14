using Discord.Interactions;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

public sealed class BaseModule : InteractionModuleBase<ShardedInteractionContext>
{
    [SlashCommand("ping", "Get the bot estimated round-trip latency, in milliseconds, to the gateway server.")]
    public async Task PingCommand()
    {
        await RespondAsync($"Ping: {Context.Client.Latency} ms").ConfigureAwait(false);
    }
}