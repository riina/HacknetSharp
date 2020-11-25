using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace hspostgres.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserModel",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Base64Salt = table.Column<string>(type: "text", nullable: false),
                    Base64Password = table.Column<string>(type: "text", nullable: false),
                    Admin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserModel", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationToken",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    ForgerKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationToken", x => x.Key);
                    table.ForeignKey(
                        name: "FK_RegistrationToken_UserModel_ForgerKey",
                        column: x => x.ForgerKey,
                        principalTable: "UserModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OwnerKey = table.Column<Guid>(type: "uuid", nullable: false),
                    World = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemModel", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "FileModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerKey = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    World = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_FileModel_SystemModel_OwnerKey",
                        column: x => x.OwnerKey,
                        principalTable: "SystemModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerModel",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    ActiveWorld = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultSystem = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentSystemKey = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_PlayerModel_SystemModel_CurrentSystemKey",
                        column: x => x.CurrentSystemKey,
                        principalTable: "SystemModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    PlayerKey = table.Column<string>(type: "text", nullable: true),
                    World = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_PersonModel_PlayerModel_PlayerKey",
                        column: x => x.PlayerKey,
                        principalTable: "PlayerModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileModel_OwnerKey",
                table: "FileModel",
                column: "OwnerKey");

            migrationBuilder.CreateIndex(
                name: "IX_PersonModel_PlayerKey",
                table: "PersonModel",
                column: "PlayerKey");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerModel_CurrentSystemKey",
                table: "PlayerModel",
                column: "CurrentSystemKey");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationToken_ForgerKey",
                table: "RegistrationToken",
                column: "ForgerKey");

            migrationBuilder.CreateIndex(
                name: "IX_SystemModel_OwnerKey",
                table: "SystemModel",
                column: "OwnerKey");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemModel_PersonModel_OwnerKey",
                table: "SystemModel",
                column: "OwnerKey",
                principalTable: "PersonModel",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerModel_SystemModel_CurrentSystemKey",
                table: "PlayerModel");

            migrationBuilder.DropTable(
                name: "FileModel");

            migrationBuilder.DropTable(
                name: "RegistrationToken");

            migrationBuilder.DropTable(
                name: "UserModel");

            migrationBuilder.DropTable(
                name: "SystemModel");

            migrationBuilder.DropTable(
                name: "PersonModel");

            migrationBuilder.DropTable(
                name: "PlayerModel");
        }
    }
}
