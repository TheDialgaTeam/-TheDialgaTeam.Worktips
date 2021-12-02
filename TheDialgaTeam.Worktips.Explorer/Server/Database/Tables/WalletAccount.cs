using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;

public class WalletAccount
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong UserId { get; set; }

    public string? RegisteredWalletAddress { get; set; }

    public uint AccountIndex { get; set; }

    public string TipWalletAddress { get; set; } = null!;
}