using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiCopierWPF.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupEntries",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    SHA512 = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    EncryptedFileName = table.Column<string>(type: "TEXT", nullable: true),
                    EncryptionKey = table.Column<byte[]>(type: "BLOB", nullable: true),
                    IV = table.Column<byte[]>(type: "BLOB", nullable: true),
                    EncryptedFilePath = table.Column<string>(type: "TEXT", nullable: true),
                    BackupTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupEntries", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupEntries");
        }
    }
}
