using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Core.Logging.Microsoft;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Grpc;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Server.Services;

namespace TheDialgaTeam.Worktips.Explorer.Server;

public static class Program
{
    [RequiresUnreferencedCode("")]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOptions<DiscordOptions>().BindConfiguration("Discord", options => options.BindNonPublicProperties = true);
        builder.Services.AddOptions<BlockchainOptions>().BindConfiguration("Blockchain", options => options.BindNonPublicProperties = true);

        builder.Services.AddDbContextFactory<SqliteDatabaseContext>(optionsBuilder => { optionsBuilder.UseSqlite($"Data Source={Path.Combine(builder.Environment.ContentRootPath, "data.db")}"); });

        builder.Services.AddGrpc();

        builder.Services.AddSingleton(service =>
        {
            var options = service.GetRequiredService<IOptions<BlockchainOptions>>();
            return new DaemonRpcClient(options.Value.Rpc.Daemon.Host, options.Value.Rpc.Daemon.Port);
        });

        builder.Services.AddSingleton(service =>
        {
            var options = service.GetRequiredService<IOptions<BlockchainOptions>>();
            return new WalletRpcClient(options.Value.Rpc.Wallet.Host, options.Value.Rpc.Wallet.Port);
        });

        builder.Services.AddSingleton(_ => new DiscordShardedClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.GuildMembers }));
        builder.Services.AddSingleton(service => new InteractionService(service.GetRequiredService<DiscordShardedClient>()));

        builder.Services.AddSingleton<HttpClient>();
        
        builder.Services.AddHostedService<DaemonHostedService>();
        builder.Services.AddHostedService<WalletHostedService>();
        builder.Services.AddHostedService<DiscordHostedService>();

        builder.Logging.AddLoggerTemplateFormatter(options => { options.SetDefaultTemplate(formattingBuilder => formattingBuilder.SetGlobal(messageFormattingBuilder => messageFormattingBuilder.SetPrefix((in LoggerTemplateEntry _) => $"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{DateTime.Now:yyyy-MM-dd HH:mm:ss}{AnsiEscapeCodeConstants.Reset} "))); });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseHsts();
        }

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseHttpsRedirection();
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