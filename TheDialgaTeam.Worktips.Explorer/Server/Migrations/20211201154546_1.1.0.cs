using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheDialgaTeam.Worktips.Explorer.Server.Migrations
{
    public partial class _110 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RegisteredWalletAddress",
                table: "WalletAccounts",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RegisteredWalletAddress",
                table: "WalletAccounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
