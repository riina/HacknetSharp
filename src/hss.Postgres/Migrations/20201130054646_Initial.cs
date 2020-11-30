using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace hss.Postgres.Migrations
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
                    Hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    Salt = table.Column<byte[]>(type: "bytea", nullable: false),
                    Admin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserModel", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "WorldModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    PlayerSystemTemplate = table.Column<string>(type: "text", nullable: false),
                    StartupProgram = table.Column<string>(type: "text", nullable: false),
                    StartupCommandLine = table.Column<string>(type: "text", nullable: false),
                    PlayerAddressRange = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldModel", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "PlayerModel",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    UserForeignKey = table.Column<string>(type: "text", nullable: false),
                    ActiveWorld = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_PlayerModel_UserModel_UserForeignKey",
                        column: x => x.UserForeignKey,
                        principalTable: "UserModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
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
                name: "FileModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemKey = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerKey = table.Column<Guid>(type: "uuid", nullable: true),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    Read = table.Column<byte>(type: "smallint", nullable: false),
                    Write = table.Column<byte>(type: "smallint", nullable: false),
                    Execute = table.Column<byte>(type: "smallint", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    WorldKey = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_FileModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoginModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldKey = table.Column<Guid>(type: "uuid", nullable: true),
                    SystemKey = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonForeignKey = table.Column<Guid>(type: "uuid", nullable: false),
                    User = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    Salt = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_LoginModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OsName = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<long>(type: "bigint", nullable: false),
                    InitialProgram = table.Column<string>(type: "text", nullable: true),
                    OwnerKey = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldKey = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_SystemModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    PlayerKey = table.Column<string>(type: "text", nullable: true),
                    DefaultSystemKey = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentSystemKey = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkingDirectory = table.Column<string>(type: "text", nullable: false),
                    WorldKey = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_PersonModel_PlayerModel_PlayerKey",
                        column: x => x.PlayerKey,
                        principalTable: "PlayerModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonModel_SystemModel_CurrentSystemKey",
                        column: x => x.CurrentSystemKey,
                        principalTable: "SystemModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonModel_SystemModel_DefaultSystemKey",
                        column: x => x.DefaultSystemKey,
                        principalTable: "SystemModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileModel_OwnerKey",
                table: "FileModel",
                column: "OwnerKey");

            migrationBuilder.CreateIndex(
                name: "IX_FileModel_SystemKey",
                table: "FileModel",
                column: "SystemKey");

            migrationBuilder.CreateIndex(
                name: "IX_FileModel_WorldKey",
                table: "FileModel",
                column: "WorldKey");

            migrationBuilder.CreateIndex(
                name: "IX_LoginModel_PersonForeignKey",
                table: "LoginModel",
                column: "PersonForeignKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginModel_SystemKey",
                table: "LoginModel",
                column: "SystemKey");

            migrationBuilder.CreateIndex(
                name: "IX_LoginModel_WorldKey",
                table: "LoginModel",
                column: "WorldKey");

            migrationBuilder.CreateIndex(
                name: "IX_PersonModel_CurrentSystemKey",
                table: "PersonModel",
                column: "CurrentSystemKey");

            migrationBuilder.CreateIndex(
                name: "IX_PersonModel_DefaultSystemKey",
                table: "PersonModel",
                column: "DefaultSystemKey");

            migrationBuilder.CreateIndex(
                name: "IX_PersonModel_PlayerKey",
                table: "PersonModel",
                column: "PlayerKey");

            migrationBuilder.CreateIndex(
                name: "IX_PersonModel_WorldKey",
                table: "PersonModel",
                column: "WorldKey");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerModel_UserForeignKey",
                table: "PlayerModel",
                column: "UserForeignKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationToken_ForgerKey",
                table: "RegistrationToken",
                column: "ForgerKey");

            migrationBuilder.CreateIndex(
                name: "IX_SystemModel_OwnerKey",
                table: "SystemModel",
                column: "OwnerKey");

            migrationBuilder.CreateIndex(
                name: "IX_SystemModel_WorldKey",
                table: "SystemModel",
                column: "WorldKey");

            migrationBuilder.AddForeignKey(
                name: "FK_FileModel_LoginModel_OwnerKey",
                table: "FileModel",
                column: "OwnerKey",
                principalTable: "LoginModel",
                principalColumn: "Key",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FileModel_SystemModel_SystemKey",
                table: "FileModel",
                column: "SystemKey",
                principalTable: "SystemModel",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LoginModel_PersonModel_PersonForeignKey",
                table: "LoginModel",
                column: "PersonForeignKey",
                principalTable: "PersonModel",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LoginModel_SystemModel_SystemKey",
                table: "LoginModel",
                column: "SystemKey",
                principalTable: "SystemModel",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_PersonModel_SystemModel_CurrentSystemKey",
                table: "PersonModel");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonModel_SystemModel_DefaultSystemKey",
                table: "PersonModel");

            migrationBuilder.DropTable(
                name: "FileModel");

            migrationBuilder.DropTable(
                name: "RegistrationToken");

            migrationBuilder.DropTable(
                name: "LoginModel");

            migrationBuilder.DropTable(
                name: "SystemModel");

            migrationBuilder.DropTable(
                name: "PersonModel");

            migrationBuilder.DropTable(
                name: "PlayerModel");

            migrationBuilder.DropTable(
                name: "WorldModel");

            migrationBuilder.DropTable(
                name: "UserModel");
        }
    }
}
