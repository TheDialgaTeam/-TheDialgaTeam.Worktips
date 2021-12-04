using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Worktips.Explorer.Server.Discord.Exchanges.TradeOgre;
using TheDialgaTeam.Worktips.Explorer.Server.Options;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

[Name("Exchange")]
public class ExchangeModule : AbstractModule
{
    private readonly BlockchainOptions _blockchainOptions;

    public ExchangeModule(IOptionsMonitor<BlockchainOptions> optionsMonitor, IHostApplicationLifetime hostApplicationLifetime, ILoggerTemplate<AbstractModule> logger) : base(hostApplicationLifetime, logger)
    {
        _blockchainOptions = optionsMonitor.CurrentValue;
    }

    [Command("TradeOgreLitecoinPrice")]
    [Alias("Price", "LTCPrice", "LitecoinPrice", "TradeOgreLTCPrice")]
    [Summary("Retrieve the volume, high, and low are in the last 24 hours, initial price is the price from 24 hours ago.")]
    public async Task TradeOgreLitecoinPriceAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            var ticker = await httpClient.GetFromJsonAsync($"https://tradeogre.com/api/v1/ticker/LTC-{_blockchainOptions.CoinTicker}", TickerContext.Default.Ticker);

            if (ticker is not { Success: true })
            {
                await ReplyAsync(":x: Unable to retrieve market information from TradeOgre.\nThis coin may not be listed.");
                return;
            }

            var priceChange = ticker.Price - ticker.InitialPrice;
            var priceChangeSign = priceChange > 0 ? "+" : string.Empty;

            var marketEmbed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle($":moneybag: TradeOgre LTC-{_blockchainOptions.CoinTicker} Price")
                .WithUrl($"https://tradeogre.com/exchange/LTC-{_blockchainOptions.CoinTicker}")
                .AddField("Current Price", $"{ticker.Price} LTC", true)
                .AddField("24h Chg.", $"{priceChange} LTC ({priceChangeSign}{priceChange / ticker.InitialPrice:P2})", true)
                .AddField("Volume", $"{ticker.Volume} LTC")
                .AddField("24h Low", $"{ticker.Low} LTC", true)
                .AddField("24h High", $"{ticker.High} LTC", true);

            await ReplyAsync(embed: marketEmbed.Build());
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }

    [Command("TradeOgreBitcoinPrice")]
    [Alias("BTCPrice", "BitcoinPrice", "TradeOgreBTCPrice")]
    [Summary("Retrieve the volume, high, and low are in the last 24 hours, initial price is the price from 24 hours ago.")]
    public async Task TradeOgreBitcoinPriceAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            var ticker = await httpClient.GetFromJsonAsync($"https://tradeogre.com/api/v1/ticker/BTC-{_blockchainOptions.CoinTicker}", TickerContext.Default.Ticker);

            if (ticker is not { Success: true })
            {
                await ReplyAsync(":x: Unable to retrieve market information from TradeOgre.\nThis coin may not be listed.");
                return;
            }

            var priceChange = ticker.Price - ticker.InitialPrice;
            var priceChangeSign = "";

            if (priceChange > 0)
            {
                priceChangeSign = "+";
            }

            var marketEmbed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle($":moneybag: TradeOgre BTC-{_blockchainOptions.CoinTicker} Price")
                .WithUrl($"https://tradeogre.com/exchange/BTC-{_blockchainOptions.CoinTicker}")
                .AddField("Current Price", $"{ticker.Price} BTC", true)
                .AddField("24h Chg.", $"{priceChange} BTC ({priceChangeSign}{priceChange / ticker.InitialPrice:P2})", true)
                .AddField("Volume", $"{ticker.Volume} BTC")
                .AddField("24h Low", $"{ticker.Low} BTC", true)
                .AddField("24h High", $"{ticker.High} BTC", true);

            await ReplyAsync("", false, marketEmbed.Build());
        }
        catch (Exception ex)
        {
            await CatchError(ex);
        }
    }
}