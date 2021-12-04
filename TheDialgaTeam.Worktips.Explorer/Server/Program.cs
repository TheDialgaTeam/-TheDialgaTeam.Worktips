using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using TheDialgaTeam.Core.Logger.Serilog.Sinks;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Discord.Command;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Server.Services;

namespace TheDialgaTeam.Worktips.Explorer.Server;

public static class Program
{
    /// <summary>
    /// FIXME: This is required for EF Core 6.0 as it is not compatible with trimming.
    /// </summary>
    [UsedImplicitly]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private static readonly Type DateOnly = typeof(DateOnly);

    public static void Main(string[] args)
    {
        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.ReadLine();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                // Options
                serviceCollection.Configure<DiscordOptions>(hostBuilderContext.Configuration.GetSection("Discord"));
                serviceCollection.Configure<BlockchainOptions>(hostBuilderContext.Configuration.GetSection("Blockchain"));

                // Database
                serviceCollection.AddDbContextFactory<SqliteDatabaseContext>(builder => { builder.UseSqlite($"Data Source={Path.Combine(hostBuilderContext.HostingEnvironment.ContentRootPath, "data.db")}"); });
                serviceCollection.AddHostedService<SqliteDatabaseHostedService>();

                // Logger
                serviceCollection.AddLoggingTemplate(new LoggerTemplateConfiguration(configuration =>
                {
                    configuration.Global.DefaultPrefixTemplate = $"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{DateTimeOffset:yyyy-MM-dd HH:mm:ss}}{AnsiEscapeCodeConstants.Reset} ";
                    configuration.Global.DefaultPrefixTemplateArgs = () => new object[] { DateTimeOffset.Now };
                }));

                // Discord
                serviceCollection.AddSingleton(_ => new DiscordShardedClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    RateLimitPrecision = RateLimitPrecision.Millisecond
                }));
                serviceCollection.AddSingleton(_ =>
                {
                    var commandService = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false, IgnoreExtraArgs = true, ThrowOnError = true });
                    commandService.AddTypeReader<IEmote>(new EmoteTypeReader());
                    return commandService;
                });
                serviceCollection.AddHostedService<DiscordHostedService>();

                // Rpc
                serviceCollection.AddSingleton(_ => new DaemonRpcClient(hostBuilderContext.Configuration["Blockchain:Rpc:Daemon:Host"], int.Parse(hostBuilderContext.Configuration["Blockchain:Rpc:Daemon:Port"])));
                serviceCollection.AddSingleton(_ => new WalletRpcClient(hostBuilderContext.Configuration["Blockchain:Rpc:Wallet:Host"], int.Parse(hostBuilderContext.Configuration["Blockchain:Rpc:Wallet:Port"])));
                serviceCollection.AddHostedService<DaemonHostedService>();
                serviceCollection.AddHostedService<WalletHostedService>();
            })
            .UseSerilog((hostBuilderContext, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(hostBuilderContext.Configuration)
                    .WriteTo.AnsiConsole(new AnsiOutputTemplateTextFormatter("{Message}{NewLine}{Exception}"));
            })
            .ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.UseStartup<Startup>());
    }
}