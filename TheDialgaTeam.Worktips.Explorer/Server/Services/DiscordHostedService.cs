using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;
using TheDialgaTeam.Worktips.Explorer.Server.Options;

namespace TheDialgaTeam.Worktips.Explorer.Server.Services;

public class DiscordHostedService : IHostedService
{
    private readonly DiscordShardedClient _discordShardedClient;
    private readonly CommandService _commandService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerTemplate<DiscordHostedService> _logger;
    private readonly IOptionsMonitor<DiscordOptions> _optionsMonitor;

    public DiscordHostedService(DiscordShardedClient discordShardedClient, CommandService commandService, IServiceProvider serviceProvider, ILoggerTemplate<DiscordHostedService> logger, IOptionsMonitor<DiscordOptions> optionsMonitor)
    {
        _discordShardedClient = discordShardedClient;
        _commandService = commandService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var discordOptions = _optionsMonitor.CurrentValue;

        await _discordShardedClient.LoginAsync(TokenType.Bot, discordOptions.BotToken);
        await _discordShardedClient.StartAsync();

        _discordShardedClient.Log += DiscordShardedClientOnLog;
        _discordShardedClient.ShardReady += DiscordShardedClientOnShardReady;
        _discordShardedClient.MessageReceived += DiscordShardedClientOnMessageReceived;

        await _commandService.AddModuleAsync<BaseModule>(_serviceProvider);
        await _commandService.AddModuleAsync<DaemonModule>(_serviceProvider);
        await _commandService.AddModuleAsync<WalletModule>(_serviceProvider);
        await _commandService.AddModuleAsync<FaucetModule>(_serviceProvider);
        await _commandService.AddModuleAsync<ExchangeModule>(_serviceProvider);
        await _commandService.AddModuleAsync<HelpModule>(_serviceProvider);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discordShardedClient.StopAsync();
        await _discordShardedClient.LogoutAsync();

        _discordShardedClient.Log -= DiscordShardedClientOnLog;
        _discordShardedClient.ShardReady -= DiscordShardedClientOnShardReady;
        _discordShardedClient.MessageReceived -= DiscordShardedClientOnMessageReceived;
    }

    private Task DiscordShardedClientOnLog(LogMessage logMessage)
    {
        var botId = _discordShardedClient.CurrentUser?.Id;
        var botName = _discordShardedClient.CurrentUser?.ToString();
        var message = _discordShardedClient.CurrentUser == null ? $"[Bot] {logMessage.Source,-12} {logMessage.Message}" : $"[Bot] {botName} ({botId}): {logMessage.Source,-12} {logMessage.Message}";

        switch (logMessage.Severity)
        {
            case LogSeverity.Verbose:
                _logger.LogTrace(message, true);
                break;

            case LogSeverity.Debug:
                _logger.LogDebug(message, true);
                break;

            case LogSeverity.Info:
                _logger.LogInformation(message, true);
                break;

            case LogSeverity.Warning:
                _logger.LogWarning(message, true);
                break;

            case LogSeverity.Error:
                _logger.LogError(logMessage.Exception, message, true);
                break;

            case LogSeverity.Critical:
                _logger.LogCritical(logMessage.Exception, message, true);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return Task.CompletedTask;
    }

    private async Task DiscordShardedClientOnShardReady(DiscordSocketClient discordSocketClient)
    {
        var currentUser = discordSocketClient.CurrentUser;
        await discordSocketClient.SetGameAsync($"{currentUser.Username} help");

        _logger.LogInformation("[Bot] {Username:l} ({Id}): \u001b[32;1mShard {CurrentShard}/{TotalShard} is ready!\u001b[0m", true, currentUser.ToString(), currentUser.Id, discordSocketClient.ShardId + 1, _discordShardedClient.Shards.Count);
    }

    private Task DiscordShardedClientOnMessageReceived(SocketMessage socketMessage)
    {
        return Task.Run(async () =>
        {
            if (socketMessage is SocketUserMessage socketUserMessage)
            {
                var context = new ShardedCommandContext(_discordShardedClient, socketUserMessage);
                var argPos = 0;
                var discordOptions = _optionsMonitor.CurrentValue;

                if (socketUserMessage.Channel is SocketDMChannel)
                {
                    if (socketUserMessage.HasMentionPrefix(_discordShardedClient.CurrentUser, ref argPos) ||
                        socketUserMessage.HasStringPrefix(discordOptions.BotPrefix, ref argPos, StringComparison.OrdinalIgnoreCase))
                    {
                    }
                }
                else
                {
                    if (!socketUserMessage.HasMentionPrefix(_discordShardedClient.CurrentUser, ref argPos) &&
                        !socketUserMessage.HasStringPrefix(discordOptions.BotPrefix, ref argPos, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                await _commandService.ExecuteAsync(context, argPos, _serviceProvider);
            }
        });
    }
}