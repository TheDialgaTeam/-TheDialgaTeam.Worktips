using System.Text;
using Discord;
using Discord.Commands;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Worktips.Explorer.Server.Discord.Command;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

[Name("Help")]
public class HelpModule : AbstractModule
{
    private readonly CommandService _commandService;
    private readonly IServiceProvider _serviceProvider;

    public HelpModule(CommandService commandService, IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime, ILoggerTemplate<AbstractModule> logger) : base(hostApplicationLifetime, logger)
    {
        _commandService = commandService;
        _serviceProvider = serviceProvider;
    }

    private static bool CheckCommandEquals(CommandInfo command, string commandName)
    {
        if (command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase)) return true;

        foreach (var commandAlias in command.Aliases)
        {
            if (commandAlias.Equals(commandName, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private static string AppendNotes(CommandInfo commandInfo)
    {
        var stringBuilder = new StringBuilder();
        var ignoredTypes = new List<Type>();

        foreach (var commandInfoParameter in commandInfo.Parameters)
        {
            switch (commandInfoParameter.Type.Name)
            {
                case nameof(Boolean) when !ignoredTypes.Contains(typeof(bool)):
                    stringBuilder.AppendLine($"{nameof(Boolean)} arguments can be true or false.\n");
                    ignoredTypes.Add(typeof(bool));
                    break;

                case nameof(Char) when !ignoredTypes.Contains(typeof(char)):
                    stringBuilder.AppendLine($"{nameof(Char)} arguments only accepts one single character without quotes.\n");
                    ignoredTypes.Add(typeof(char));
                    break;

                case nameof(SByte) when !ignoredTypes.Contains(typeof(sbyte)):
                    stringBuilder.AppendLine($"{nameof(SByte)} arguments only accepts {sbyte.MinValue} to {sbyte.MaxValue}.\n");
                    ignoredTypes.Add(typeof(sbyte));
                    break;

                case nameof(Byte) when !ignoredTypes.Contains(typeof(byte)):
                    stringBuilder.AppendLine($"{nameof(Byte)} arguments only accepts {byte.MinValue} to {byte.MaxValue}.\n");
                    ignoredTypes.Add(typeof(byte));
                    break;

                case nameof(UInt16) when !ignoredTypes.Contains(typeof(ushort)):
                    stringBuilder.AppendLine($"{nameof(UInt16)} arguments only accepts {ushort.MinValue} to {ushort.MaxValue}.\n");
                    ignoredTypes.Add(typeof(ushort));
                    break;

                case nameof(Int16) when !ignoredTypes.Contains(typeof(short)):
                    stringBuilder.AppendLine($"{nameof(Int16)} arguments only accepts {short.MinValue} to {short.MaxValue}.\n");
                    ignoredTypes.Add(typeof(short));
                    break;

                case nameof(UInt32) when !ignoredTypes.Contains(typeof(uint)):
                    stringBuilder.AppendLine($"{nameof(UInt32)} arguments only accepts {uint.MinValue} to {uint.MaxValue}.\n");
                    ignoredTypes.Add(typeof(uint));
                    break;

                case nameof(Int32) when !ignoredTypes.Contains(typeof(int)):
                    stringBuilder.AppendLine($"{nameof(Int32)} arguments only accepts {int.MinValue} to {int.MaxValue}.\n");
                    ignoredTypes.Add(typeof(int));
                    break;

                case nameof(UInt64) when !ignoredTypes.Contains(typeof(ulong)):
                    stringBuilder.AppendLine($"{nameof(UInt64)} arguments only accepts {ulong.MinValue} to {ulong.MaxValue}.\n");
                    ignoredTypes.Add(typeof(ulong));
                    break;

                case nameof(Int64) when !ignoredTypes.Contains(typeof(long)):
                    stringBuilder.AppendLine($"{nameof(Int64)} arguments only accepts {long.MinValue} to {long.MaxValue}.\n");
                    ignoredTypes.Add(typeof(long));
                    break;

                case nameof(Single) when !ignoredTypes.Contains(typeof(float)):
                    stringBuilder.AppendLine($"{nameof(Single)} arguments only accepts {float.MinValue} to {float.MaxValue}.\n");
                    ignoredTypes.Add(typeof(float));
                    break;

                case nameof(Double) when !ignoredTypes.Contains(typeof(double)):
                    stringBuilder.AppendLine($"{nameof(Double)} arguments only accepts {double.MinValue} to {double.MaxValue}.\n");
                    ignoredTypes.Add(typeof(double));
                    break;

                case nameof(Decimal) when !ignoredTypes.Contains(typeof(decimal)):
                    stringBuilder.AppendLine($"{nameof(Decimal)} arguments only accepts {decimal.MinValue} to {decimal.MaxValue}.\n");
                    ignoredTypes.Add(typeof(decimal));
                    break;

                case nameof(String) when !ignoredTypes.Contains(typeof(string)):
                    stringBuilder.AppendLine($"{nameof(String)} arguments must be double quoted except for the remainder string type.\n");
                    ignoredTypes.Add(typeof(string));
                    break;

                case nameof(TimeSpan) when !ignoredTypes.Contains(typeof(TimeSpan)):
                    stringBuilder.AppendLine($@"{nameof(TimeSpan)} arguments must be in one of these format:
`#d#h#m#s`, `#d#h#m`, `#d#h#s`, `#d#h`, `#d#m#s`, `#d#m`, `#d#s`, `#d`, `#h#m#s`, `#h#m`, `#h#s`, `#h`, `#m#s`, `#m`, `#s`

#: Number of units. (d,h,m,s)
d: Days, ranging from 0 to 10675199.
h: Hours, ranging from 0 to 23.
m: Minutes, ranging from 0 to 59.
s: Optional seconds, ranging from 0 to 59.
");
                    ignoredTypes.Add(typeof(TimeSpan));
                    break;

                case nameof(IChannel) when !ignoredTypes.Contains(typeof(IChannel)):
                    stringBuilder.AppendLine($"{nameof(IChannel)} arguments can be #channel or channel id or channel name.\n");
                    ignoredTypes.Add(typeof(IChannel));
                    break;

                case nameof(IUser) when !ignoredTypes.Contains(typeof(IUser)):
                    stringBuilder.AppendLine($"{nameof(IUser)} arguments can be @user or user id or username or nickname.\n");
                    ignoredTypes.Add(typeof(IUser));
                    break;

                case nameof(IRole) when !ignoredTypes.Contains(typeof(IRole)):
                    stringBuilder.AppendLine($"{nameof(IRole)} arguments can be @role or role id or role name.\n");
                    ignoredTypes.Add(typeof(IRole));
                    break;

                case nameof(IEmote) when !ignoredTypes.Contains(typeof(IEmote)):
                    stringBuilder.AppendLine($"{nameof(IEmote)} arguments is discord emojis.\n");
                    ignoredTypes.Add(typeof(IEmote));
                    break;
            }
        }

        return stringBuilder.ToString();
    }

    [Command("Help")]
    public async Task HelpAsync([Remainder] string commandName)
    {
        try
        {
            foreach (var commandServiceModule in _commandService.Modules)
            {
                if (commandServiceModule.Name.Equals("Help", StringComparison.OrdinalIgnoreCase)) continue;

                foreach (var command in commandServiceModule.Commands)
                {
                    if (!CheckCommandEquals(command, commandName)) continue;

                    var clientContext = Context.Client;

                    var helpMessage = new EmbedBuilder()
                        .WithTitle("Command Info:")
                        .WithColor(Color.Orange)
                        .WithDescription($"To find out more about each command, use `@{clientContext.CurrentUser} help <CommandName>`\nIn DM, you can use `help <CommandName>`");

                    var requiredPermission = RequiredPermission.GuildMember;
                    var requiredContext = ContextType.Guild | ContextType.DM | ContextType.Group;

                    foreach (var commandAttribute in command.Preconditions)
                    {
                        switch (commandAttribute)
                        {
                            case RequirePermissionAttribute requirePermissionAttribute:
                                requiredPermission = requirePermissionAttribute.RequiredPermission;
                                break;

                            case RequireContextAttribute requireContextAttribute:
                                requiredContext = requireContextAttribute.Contexts;
                                break;
                        }
                    }

                    var requiredContexts = new List<string>();

                    if ((requiredContext & ContextType.Guild) == ContextType.Guild)
                    {
                        requiredContexts.Add(ContextType.Guild.ToString());
                    }

                    if ((requiredContext & ContextType.DM) == ContextType.DM)
                    {
                        requiredContexts.Add(ContextType.DM.ToString());
                    }

                    var requiredContextString = string.Join(", ", requiredContexts);

                    var commandInfo = new StringBuilder($"Usage: {clientContext.CurrentUser.Mention} {command.Name}");
                    var argsInfo = new StringBuilder();

                    foreach (var commandParameter in command.Parameters)
                    {
                        if (commandParameter.IsMultiple)
                        {
                            commandInfo.Append($" `<params {commandParameter.Type.Name}[] {commandParameter.Name}>`");
                        }
                        else if (commandParameter.IsOptional)
                        {
                            commandInfo.Append($" `[{(commandParameter.IsRemainder ? "Remainder " : string.Empty)}{commandParameter.Type.Name} {commandParameter.Name} = {commandParameter.DefaultValue ?? "null"}]`");
                        }
                        else
                        {
                            commandInfo.Append($" `<{(commandParameter.IsRemainder ? "Remainder " : string.Empty)}{commandParameter.Type.Name} {commandParameter.Name}>`");
                        }

                        argsInfo.AppendLine($"{commandParameter.Type.Name} {commandParameter.Name}: {commandParameter.Summary}");
                    }

                    commandInfo.AppendLine($"\nDescription: {command.Summary}");
                    commandInfo.AppendLine($"Required Permission: {requiredPermission}");
                    commandInfo.AppendLine($"Required Context: {requiredContextString}");

                    if (argsInfo.Length > 0)
                    {
                        commandInfo.AppendLine("\nArguments Info:");
                        commandInfo.Append(argsInfo);
                    }

                    foreach (var commandAttribute in command.Attributes)
                    {
                        if (commandAttribute is not ExampleAttribute exampleAttribute) continue;

                        commandInfo.AppendLine("\nExample:");
                        commandInfo.AppendLine(exampleAttribute.Message);
                    }

                    var notes = AppendNotes(command);

                    if (!string.IsNullOrEmpty(notes))
                    {
                        commandInfo.AppendLine("\nNote:");
                        commandInfo.Append(notes);
                    }

                    helpMessage.AddField($"{command.Name} command:", commandInfo.ToString());

                    await ReplyAsync(embed: helpMessage.Build());
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("Help")]
    public async Task HelpAsync()
    {
        try
        {
            var currentUser = Context.Client.CurrentUser;

            var helpMessage = new EmbedBuilder()
                .WithTitle("Available Command:")
                .WithColor(Color.Orange)
                .WithDescription($"To find out more about each command, use `@{currentUser} help <CommandName>`\nIn DM, you can use `help <CommandName>`")
                .WithThumbnailUrl(currentUser.GetAvatarUrl());

            foreach (var module in _commandService.Modules)
            {
                if (module.Name.Equals("Help", StringComparison.OrdinalIgnoreCase)) continue;

                var commandInfo = new StringBuilder();

                foreach (var command in module.Commands)
                {
                    var preconditionResult = await command.CheckPreconditionsAsync(Context, _serviceProvider);

                    if (!preconditionResult.IsSuccess) continue;

                    commandInfo.Append($"`{command.Name}`");

                    if (command.Aliases.Count > 0)
                    {
                        foreach (var commandAlias in command.Aliases)
                        {
                            if (!commandAlias.Equals(command.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                commandInfo.Append($", `{commandAlias}`");
                            }
                        }
                    }

                    commandInfo.AppendLine($": {command.Summary}");
                }

                if (commandInfo.Length > 0)
                {
                    helpMessage.AddField($"{module.Name} Module", commandInfo.ToString());
                }
            }

            await ReplyAsync(embed: helpMessage.Build());
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }
}