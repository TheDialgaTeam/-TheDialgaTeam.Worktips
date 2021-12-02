using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheDialgaTeam.Worktips.Explorer.Server.Migrations
{
    public partial class _100 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DaemonSyncHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlockCount = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TotalCirculation = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DaemonSyncHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaucetHistories",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaucetHistories", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WalletAccounts",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AccountIndex = table.Column<uint>(type: "INTEGER", nullable: false),
                    RegisteredWalletAddress = table.Column<string>(type: "TEXT", nullable: false),
                    TipWalletAddress = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletAccounts", x => x.UserId);
                });

            migrationBuilder.InsertData(
                table: "DaemonSyncHistory",
                columns: new[] { "Id", "BlockCount", "TotalCirculation" },
                values: new object[] { 1, 0ul, 0ul });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DaemonSyncHistory");

            migrationBuilder.DropTable(
                name: "FaucetHistories");

            migrationBuilder.DropTable(
                name: "WalletAccounts");
        }
    }
}
