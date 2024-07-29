using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheDialgaTeam.Worktips.Explorer.Server.Migrations
{
    /// <inheritdoc />
    public partial class _130 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SentToRegisteredWalletDirectly",
                table: "WalletAccounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SentToRegisteredWalletDirectly",
                table: "WalletAccounts");
        }
    }
}
