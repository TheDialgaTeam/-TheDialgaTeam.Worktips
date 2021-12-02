using System.Text.Json.Serialization;

namespace TheDialgaTeam.Worktips.Explorer.Server.Discord.Exchanges.TradeOgre;

public class Ticker
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("initialprice")]
    public decimal InitialPrice { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("high")]
    public decimal High { get; set; }

    [JsonPropertyName("low")]
    public decimal Low { get; set; }

    [JsonPropertyName("volume")]
    public decimal Volume { get; set; }

    [JsonPropertyName("bid")]
    public decimal Bid { get; set; }

    [JsonPropertyName("ask")]
    public decimal Ask { get; set; }
}

[JsonSerializable(typeof(Ticker), GenerationMode = JsonSourceGenerationMode.Metadata)]
internal partial class TickerContext : JsonSerializerContext
{
}