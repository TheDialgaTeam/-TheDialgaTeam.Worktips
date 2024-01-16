using System.Net.Http.Headers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Serilog.Extensions;
using TheDialgaTeam.Serilog.Formatting;
using TheDialgaTeam.Serilog.Sinks.AnsiConsole;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Grpc;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Server.Services;
using TheDialgaTeam.Worktips.Explorer.Server.Utilities;

namespace TheDialgaTeam.Worktips.Explorer.Server;

internal static class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;

        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.ConfigureSerilog(static (_, _, logger) => logger.WriteTo.AnsiConsoleSink(static optionsBuilder => optionsBuilder.SetDefault(static templateBuilder => templateBuilder.SetDefault($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{Timestamp:yyyy-MM-dd HH:mm:ss}}{AnsiEscapeCodeConstants.Reset} {{Message:l}}{{NewLine}}{{Exception}}"))));

        builder.Services.AddOptions<DiscordOptions>().BindConfiguration("Discord");
        builder.Services.AddOptions<BlockchainOptions>().BindConfiguration("Blockchain");

        builder.Services.AddDbContextFactory<SqliteDatabaseContext>();

        builder.Services.AddGrpc();

        builder.Services.AddSingleton(static service =>
        {
            var options = service.GetRequiredService<IOptions<BlockchainOptions>>();
            return new DaemonRpcClient(options.Value.Rpc.Daemon.Host, options.Value.Rpc.Daemon.Port);
        });

        builder.Services.AddSingleton(static service =>
        {
            var options = service.GetRequiredService<IOptions<BlockchainOptions>>();
            return new WalletRpcClient(options.Value.Rpc.Wallet.Host, options.Value.Rpc.Wallet.Port);
        });

        builder.Services.AddSingleton(static _ => new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers }));
        builder.Services.AddSingleton(static service => new InteractionService(service.GetRequiredService<DiscordSocketClient>()));

        builder.Services.AddSingleton(static _ => new HttpClient { DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue(ApplicationUtility.Name, ApplicationUtility.Version) } } });

        builder.Services.AddHostedService<DaemonHostedService>();
        builder.Services.AddHostedService<WalletHostedService>();
        builder.Services.AddHostedService<DiscordHostedService>();
        
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
        app.MapGrpcService<DaemonService>();
        app.MapFallbackToFile("index.html");

        app.Run();
    }

    private static void OnCurrentDomainOnUnhandledException(object _, UnhandledExceptionEventArgs eventArgs)
    {
        if (!eventArgs.IsTerminating) return;

        var crashFileLocation = Path.Combine(AppContext.BaseDirectory, $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_crash.log");
        File.WriteAllText(crashFileLocation, eventArgs.ExceptionObject.ToString());
    }
}