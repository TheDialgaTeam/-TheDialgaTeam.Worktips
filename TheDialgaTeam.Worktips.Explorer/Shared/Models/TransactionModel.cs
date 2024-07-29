using System.Text.Json.Serialization;

namespace TheDialgaTeam.Worktips.Explorer.Shared.Models;

[JsonSerializable(typeof(TransactionModel), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class TransactionModelContext : JsonSerializerContext;

public sealed class TransactionModel
{
    public sealed class TransactionModel_Vin
    {
        public sealed class Vin_Key
        {
            [JsonPropertyName("amount")]
            public ulong Amount { get; set; }

            [JsonPropertyName("key_offsets")]
            public ulong[] KeyOffsets { get; set; } = null!;

            [JsonPropertyName("k_image")]
            public string KeyImage { get; set; } = null!;
        }

        [JsonPropertyName("key")]
        public Vin_Key Key { get; set; } = null!;
    }

    public sealed class TransactionModel_Vout
    {
        public sealed class Vout_Target
        {
            [JsonPropertyName("key")]
            public string Key { get; set; } = null!;
        }

        [JsonPropertyName("amount")]
        public ulong Amount { get; set; }

        [JsonPropertyName("target")]
        public Vout_Target Target { get; set; } = null!;
    }

    public sealed class TransactionModel_RingSignatures
    {
        public sealed class RingSignatures_EcdhInfo
        {
            public string Amount { get; set; } = null!;
        }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("txnFee")]
        public ulong TransactionFee { get; set; }

        [JsonPropertyName("ecdhInfo")]
        public RingSignatures_EcdhInfo[] EcdhInfos { get; set; } = null!;

        [JsonPropertyName("outPk")]
        public string[] OutPublicKeys { get; set; } = null!;
    }

    [JsonPropertyName("version")]
    public byte Version { get; set; }

    [JsonPropertyName("output_unlock_times")]
    public ulong[] OutputUnlockTimes { get; set; } = null!;

    [JsonPropertyName("unlock_time")]
    public ulong UnlockTime { get; set; }

    [JsonPropertyName("vin")]
    public TransactionModel_Vin[] Vin { get; set; } = null!;

    [JsonPropertyName("vout")]
    public TransactionModel_Vout[] Vout { get; set; } = null!;

    [JsonPropertyName("extra")]
    public int[] Extras { get; set; } = null!;

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("rct_signatures")]
    public TransactionModel_RingSignatures RingSignatures { get; set; } = null!;
}