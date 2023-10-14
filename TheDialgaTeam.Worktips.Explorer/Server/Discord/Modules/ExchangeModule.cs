using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Worktips.Explorer.Server.Discord.Exchanges.TradeOgre;
using TheDialgaTeam.Worktips.Explorer.Server.Options;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Modules;

public sealed class ExchangeModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly BlockchainOptions _blockchainOptions;
    private readonly HttpClient _httpClient;

    public ExchangeModule(IOptions<BlockchainOptions> options, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _blockchainOptions = options.Value;
    }

    [SlashCommand("price", "Retrieve the trading price for this coin.")]
    public async Task PriceCommand(
        [Choice("Litecoin (Tradeogre)", "LTC")] [Choice("Bitcoin (Tradeogre)", "BTC")]
        [Summary("type", "Type of price to retrieve.")]
        string type = "LTC")
    {
        await DeferAsync().ConfigureAwait(false);
        
        var ticker = await _httpClient.GetFromJsonAsync($"https://tradeogre.com/api/v1/ticker/{_blockchainOptions.CoinTicker}-{type}", TickerContext.Default.Ticker).ConfigureAwait(false);

        if (ticker is not { Success: true })
        {
            await FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription("Unable to retrieve market information from TradeOgre.")
                .Build()).ConfigureAwait(false);
            
            return;
        }

        var priceChange = ticker.Price - ticker.InitialPrice;
        var priceChangeSign = priceChange > 0 ? "+" : string.Empty;
        
        await FollowupAsync(embed: new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithTitle($"TradeOgre {_blockchainOptions.CoinTicker}-{type} Price")
            .AddField("Current Price", $"{ticker.Price} {type}", true)
            .AddField("24h Chg.", $"{priceChange} {type} ({priceChangeSign}{priceChange / ticker.InitialPrice:P2})", true)
            .AddField("Volume", $"{ticker.Volume} {type}")
            .AddField("24h Low", $"{ticker.Low} {type}", true)
            .AddField("24h High", $"{ticker.High} {type}", true)
            .WithUrl($"https://tradeogre.com/exchange/{_blockchainOptions.CoinTicker}-{type}")
            .Build()).ConfigureAwait(false);
    }
}