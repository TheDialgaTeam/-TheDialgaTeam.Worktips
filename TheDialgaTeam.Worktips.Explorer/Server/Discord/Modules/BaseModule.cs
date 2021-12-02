using Discord;
using Discord.Commands;
using TheDialgaTeam.Core.Logger.Extensions.Logging;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

[Name("Base")]
public class BaseModule : AbstractModule
{
    public BaseModule(IHostApplicationLifetime hostApplicationLifetime, ILoggerTemplate<AbstractModule> logger) : base(hostApplicationLifetime, logger)
    {
    }

    [Command("Ping")]
    [Summary("Gets the estimated round-trip latency, in milliseconds, to the gateway server.")]
    public async Task PingAsync()
    {
        try
        {
            await ReplyAsync($"Ping: {Context.Client.Latency} ms");
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("About")]
    [Summary("Get the bot information.")]
    public async Task AboutAsync()
    {
        try
        {
            var clientContext = Context.Client;
            var applicationInfo = await clientContext.GetApplicationInfoAsync();
            var currentUser = clientContext.CurrentUser;

            var helpMessage = new EmbedBuilder()
                .WithTitle("Tip Bot:")
                .WithThumbnailUrl(currentUser.GetAvatarUrl())
                .WithColor(Color.Orange)
                .WithDescription($@"Hello, I am **{currentUser.Username}**, a bot that is created by jianmingyong#4964.

I am owned by **{applicationInfo.Owner}**.

Type `@{clientContext.CurrentUser} help` to see my command.");

            await ReplyAsync(embed: helpMessage.Build());
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }
}