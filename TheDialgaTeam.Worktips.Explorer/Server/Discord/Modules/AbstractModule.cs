using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Wallet;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

public abstract class AbstractModule : ModuleBase<ShardedCommandContext>
{
    private readonly ILoggerTemplate<AbstractModule> _logger;
    private readonly RequestOptions _requestOptions;

    protected AbstractModule(IHostApplicationLifetime hostApplicationLifetime, ILoggerTemplate<AbstractModule> logger)
    {
        _logger = logger;
        _requestOptions = new RequestOptions { CancelToken = hostApplicationLifetime.ApplicationStopping };
    }

    protected static async Task<WalletAccount> GetOrCreateWalletAccountAsync(ulong userId, IDbContextFactory<SqliteDatabaseContext> dbContextFactory, WalletRpcClient walletRpcClient)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var result = await dbContext.WalletAccounts.FindAsync(userId);
        if (result != null) return result;

        var newWallet = await walletRpcClient.CreateAccountAsync(new CommandRpcCreateAccount.Request { Label = userId.ToString() });
        if (newWallet == null) throw new Exception();

        var walletAccount = new WalletAccount
        {
            UserId = userId,
            RegisteredWalletAddress = null,
            AccountIndex = newWallet.AccountIndex,
            TipWalletAddress = newWallet.Address
        };

        dbContext.WalletAccounts.Add(walletAccount);
        await dbContext.SaveChangesAsync();

        return walletAccount;
    }

    protected override async Task<IUserMessage?> ReplyAsync(string? message = null, bool isTextToSpeech = false, Embed? embed = null, RequestOptions? options = null, AllowedMentions? allowedMentions = null, MessageReference? messageReference = null)
    {
        var channelContext = Context.Channel;

        if (Context.Message.Channel is SocketDMChannel)
        {
            return await channelContext.SendMessageAsync(message, isTextToSpeech, embed, options ?? _requestOptions, allowedMentions, messageReference);
        }

        var guildContext = Context.Guild;

        if (guildContext.GetUser(Context.Client.CurrentUser.Id).GetPermissions(guildContext.GetChannel(channelContext.Id)).SendMessages)
        {
            return await channelContext.SendMessageAsync(message, isTextToSpeech, embed, options ?? _requestOptions, allowedMentions, messageReference);
        }

        return null;
    }

    protected async Task<IUserMessage?> ReplyToDirectMessageAsync(string? message = null, bool isTextToSpeech = false, Embed? embed = null, RequestOptions? options = null, AllowedMentions? allowedMentions = null, MessageReference? messageReference = null)
    {
        var messageContext = Context.Message;

        if (messageContext.Channel is SocketDMChannel)
        {
            return await ReplyAsync(message, isTextToSpeech, embed, options ?? _requestOptions, allowedMentions, messageReference);
        }

        var dmChannel = await messageContext.Author.GetOrCreateDMChannelAsync(options ?? _requestOptions);
        return await dmChannel.SendMessageAsync(message, isTextToSpeech, embed, options ?? _requestOptions, allowedMentions, messageReference);
    }

    protected async Task AddReactionAsync(string emoji, RequestOptions? options = null)
    {
        if (Context.Message.Channel is SocketDMChannel)
        {
            await Context.Message.AddReactionAsync(new Emoji(emoji), options ?? _requestOptions);
            return;
        }

        if (GetChannelPermissions().AddReactions)
        {
            await Context.Message.AddReactionAsync(new Emoji(emoji));
        }
    }

    protected ChannelPermissions GetChannelPermissions(ulong? channelId = null)
    {
        return Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Guild.GetChannel(channelId ?? Context.Channel.Id));
    }

    protected async Task DeleteMessageAsync()
    {
        if (Context.Message.Channel is SocketDMChannel) return;

        if (GetChannelPermissions().ManageMessages)
        {
            await Context.Message.DeleteAsync();
        }
    }

    protected async Task CatchError(Exception ex)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle("Oops, this command resulted in an error:")
            .WithColor(Color.Red)
            .WithDescription(ex.Message)
            .WithFooter("More information have been logged in the bot logger.")
            .WithTimestamp(DateTimeOffset.Now);

        await ReplyAsync(embed: embedBuilder.Build());

        _logger.LogError(ex, "Oops, this command resulted in an error:", true);
    }
}