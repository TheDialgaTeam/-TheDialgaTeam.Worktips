using Discord.Commands;
using Discord.WebSocket;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Command;

public class RequirePermissionAttribute : PreconditionAttribute
{
    public RequiredPermission RequiredPermission { get; }

    public RequirePermissionAttribute(RequiredPermission requiredPermission)
    {
        RequiredPermission = requiredPermission;
    }

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var currentUserPermission = RequiredPermission.GuildMember;
        var contextUser = context.User;

        if (context.Message.Channel is SocketGuildChannel)
        {
            var guildUser = await context.Guild.GetUserAsync(contextUser.Id);

            // Guild Administrator
            if (guildUser.GuildPermissions.Administrator)
            {
                currentUserPermission = RequiredPermission.GuildAdministrator;
            }
        }

        // Discord App Owner
        var botOwner = (await context.Client.GetApplicationInfoAsync()).Owner;

        if (contextUser.Id == botOwner.Id)
        {
            currentUserPermission = RequiredPermission.DiscordAppOwner;
        }

        return currentUserPermission >= RequiredPermission ? PreconditionResult.FromSuccess() : PreconditionResult.FromError($"This command require {RequiredPermission} permission and above.");
    }
}