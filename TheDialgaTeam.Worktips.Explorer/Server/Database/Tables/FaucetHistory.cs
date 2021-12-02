using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;

public class FaucetHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong UserId { get; set; }

    public DateTimeOffset DateTime { get; set; }
}