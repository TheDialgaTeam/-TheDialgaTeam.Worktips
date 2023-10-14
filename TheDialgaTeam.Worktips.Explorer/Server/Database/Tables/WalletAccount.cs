using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;

public sealed class WalletAccount
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong UserId { get; set; }

    public uint AccountIndex { get; set; }
    
    public string TipBotWalletAddress { get; set; } = null!;
    
    public string? RegisteredWalletAddress { get; set; }
}