using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using IResult = Discord.Interactions.IResult;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

public sealed class DiscordHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordHostedService> _logger;
    private readonly DiscordOptions _discordOptions;
    private readonly DiscordShardedClient _discordShardedClient;
    private readonly InteractionService _interactionService;
    
    public DiscordHostedService(IServiceProvider serviceProvider, ILogger<DiscordHostedService> logger, IOptions<DiscordOptions> options, DiscordShardedClient discordShardedClient, InteractionService interactionService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _discordOptions = options.Value;
        _discordShardedClient = discordShardedClient;
        _interactionService = interactionService;
    }

    private async Task InteractionServiceOnInteractionExecuted(ICommandInfo command, IInteractionContext context, IResult result)
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_discordOptions.BotToken)) return;

        await _discordShardedClient.LoginAsync(TokenType.Bot, _discordOptions.BotToken).ConfigureAwait(false);
        await _discordShardedClient.StartAsync().ConfigureAwait(false);
        
        _discordShardedClient.ShardReady += DiscordShardedClientOnShardReady;
        _discordShardedClient.InteractionCreated += DiscordShardedClientOnInteractionCreated;
        _interactionService.InteractionExecuted += InteractionServiceOnInteractionExecuted;
        
        await _interactionService.AddModuleAsync<BaseModule>(_serviceProvider).ConfigureAwait(false);
        await _interactionService.AddModuleAsync<DaemonModule>(_serviceProvider).ConfigureAwait(false);
        await _interactionService.AddModuleAsync<ExchangeModule>(_serviceProvider).ConfigureAwait(false);
        await _interactionService.AddModuleAsync<WalletModule>(_serviceProvider).ConfigureAwait(false);
        await _interactionService.AddModuleAsync<FaucetModule>(_serviceProvider).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_discordOptions.BotToken)) return;

        await _discordShardedClient.StopAsync().ConfigureAwait(false);
        await _discordShardedClient.LogoutAsync().ConfigureAwait(false);

        _discordShardedClient.ShardReady -= DiscordShardedClientOnShardReady;
        _discordShardedClient.InteractionCreated -= DiscordShardedClientOnInteractionCreated;
        _interactionService.InteractionExecuted -= InteractionServiceOnInteractionExecuted;
    }

    private async Task DiscordShardedClientOnShardReady(DiscordSocketClient discordSocketClient)
    {
#if DEBUG
        foreach (var guild in discordSocketClient.Guilds)
        {
            await _interactionService.RegisterCommandsToGuildAsync(guild.Id).ConfigureAwait(false);
        }
#else
        foreach (var guild in discordSocketClient.Guilds)
        {
            await _interactionService.RemoveModulesFromGuildAsync(guild.Id, _interactionService.Modules.ToArray()).ConfigureAwait(false);
        }

        await _interactionService.RegisterCommandsGloballyAsync().ConfigureAwait(false);
#endif
    }

    private async Task DiscordShardedClientOnInteractionCreated(SocketInteraction interaction)
    {
        var context = new ShardedInteractionContext(_discordShardedClient, interaction);
        await _interactionService.ExecuteCommandAsync(context, _serviceProvider).ConfigureAwait(false);
    }
}