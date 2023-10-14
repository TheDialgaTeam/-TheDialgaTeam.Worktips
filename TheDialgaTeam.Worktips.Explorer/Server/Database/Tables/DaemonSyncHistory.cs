using System.ComponentModel.DataAnnotations;

namespace TheDialgaTeam.Worktips.Explorer.Server.Database.Tables;

public sealed class DaemonSyncHistory
{
    [Key]
    public int Id { get; set; }
    
    public ulong BlockCount { get; set; }

    public ulong TotalCirculation { get; set; }
}