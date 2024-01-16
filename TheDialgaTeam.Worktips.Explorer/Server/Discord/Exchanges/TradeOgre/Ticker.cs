using System.Text.Json.Serialization;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Exchanges.TradeOgre;

internal class Ticker
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonPropertyName("initialprice")]
    public decimal InitialPrice { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonPropertyName("high")]
    public decimal High { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonPropertyName("low")]
    public decimal Low { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonPropertyName("volume")]
    public decimal Volume { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonPropertyName("bid")]
    public decimal Bid { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonPropertyName("ask")]
    public decimal Ask { get; set; }
}

[JsonSerializable(typeof(Ticker), GenerationMode = JsonSourceGenerationMode.Metadata)]
internal partial class TickerContext : JsonSerializerContext;