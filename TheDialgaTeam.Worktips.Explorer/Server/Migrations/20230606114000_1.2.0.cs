using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheDialgaTeam.Worktips.Explorer.Server.Migrations
{
    /// <inheritdoc />
    public partial class _120 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TipWalletAddress",
                table: "WalletAccounts",
                newName: "TipBotWalletAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TipBotWalletAddress",
                table: "WalletAccounts",
                newName: "TipWalletAddress");
        }
    }
}
