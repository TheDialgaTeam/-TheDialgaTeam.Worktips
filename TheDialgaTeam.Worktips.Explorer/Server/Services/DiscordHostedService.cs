using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using IResult = Discord.Interactions.IResult;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

internal sealed class DiscordHostedService(
    IServiceProvider serviceProvider,
    IOptions<DiscordOptions> options, 
    DiscordSocketClient discordSocketClient, 
    InteractionService interactionService) : IHostedService
{
    private readonly DiscordOptions _discordOptions = options.Value;

    private static async Task InteractionServiceOnInteractionExecuted(ICommandInfo command, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess) return;
        
        var embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Oops, this command resulted in an error:")
            .WithDescription(result.ErrorReason);
        
        if (context.Interaction.HasResponded)
        {
            await context.Interaction.FollowupAsync(embed: embed.Build()).ConfigureAwait(false);
        }
        else
        {
            await context.Interaction.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BaseModule))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DaemonModule))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ExchangeModule))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WalletModule))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FaucetModule))]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_discordOptions.BotToken)) return;

        await discordSocketClient.LoginAsync(TokenType.Bot, _discordOptions.BotToken).ConfigureAwait(false);
        await discordSocketClient.StartAsync().ConfigureAwait(false);
        
        //discordSocketClient.Ready += DiscordShardedClientOnShardReady;
        discordSocketClient.InteractionCreated += DiscordShardedClientOnInteractionCreated;
        interactionService.InteractionExecuted += InteractionServiceOnInteractionExecuted;
        
        await interactionService.AddModuleAsync<BaseModule>(serviceProvider).ConfigureAwait(false);
        await interactionService.AddModuleAsync<DaemonModule>(serviceProvider).ConfigureAwait(false);
        await interactionService.AddModuleAsync<ExchangeModule>(serviceProvider).ConfigureAwait(false);
        await interactionService.AddModuleAsync<WalletModule>(serviceProvider).ConfigureAwait(false);
        await interactionService.AddModuleAsync<FaucetModule>(serviceProvider).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_discordOptions.BotToken)) return;

        await discordSocketClient.StopAsync().ConfigureAwait(false);
        await discordSocketClient.LogoutAsync().ConfigureAwait(false);

        //discordSocketClient.Ready -= DiscordShardedClientOnShardReady;
        discordSocketClient.InteractionCreated -= DiscordShardedClientOnInteractionCreated;
        interactionService.InteractionExecuted -= InteractionServiceOnInteractionExecuted;
    }

    private async Task DiscordShardedClientOnShardReady()
    {
#if DEBUG
        foreach (var guild in discordSocketClient.Guilds)
        {
            await interactionService.RegisterCommandsToGuildAsync(guild.Id).ConfigureAwait(false);
        }
#else
        foreach (var guild in discordSocketClient.Guilds)
        {
            await interactionService.RemoveModulesFromGuildAsync(guild.Id, interactionService.Modules.ToArray()).ConfigureAwait(false);
        }

        await interactionService.RegisterCommandsGloballyAsync().ConfigureAwait(false);
#endif
    }

    private async Task DiscordShardedClientOnInteractionCreated(SocketInteraction interaction)
    {
        var context = new InteractionContext(discordSocketClient, interaction);
        await interactionService.ExecuteCommandAsync(context, serviceProvider).ConfigureAwait(false);
    }
}