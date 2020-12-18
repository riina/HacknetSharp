using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace hss.Sqlite.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserModel",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Salt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Admin = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordResetToken = table.Column<string>(type: "TEXT", nullable: true),
                    PasswordResetTokenExpiry = table.Column<long>(type: "INTEGER", nullable: false),
                    ActiveWorld = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_UserModel", x => x.Key); });

            migrationBuilder.CreateTable(
                name: "WorldModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    PlayerSystemTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    StartupCommandLine = table.Column<string>(type: "TEXT", nullable: false),
                    PlayerAddressRange = table.Column<string>(type: "TEXT", nullable: false),
                    RebootDuration = table.Column<double>(type: "REAL", nullable: false),
                    DiskCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    Now = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_WorldModel", x => x.Key); });

            migrationBuilder.CreateTable(
                name: "RegistrationToken",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    ForgerKey = table.Column<string>(type: "TEXT", nullable: true)
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
                name: "PersonModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultSystem = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    RebootDuration = table.Column<double>(type: "REAL", nullable: false),
                    DiskCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    WorldKey = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_PersonModel_UserModel_UserKey",
                        column: x => x.UserKey,
                        principalTable: "UserModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OsName = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<uint>(type: "INTEGER", nullable: false),
                    ConnectCommandLine = table.Column<string>(type: "TEXT", nullable: true),
                    BootTime = table.Column<double>(type: "REAL", nullable: false),
                    RequiredExploits = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    RebootDuration = table.Column<double>(type: "REAL", nullable: false),
                    DiskCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    FirewallIterations = table.Column<int>(type: "INTEGER", nullable: false),
                    FirewallLength = table.Column<int>(type: "INTEGER", nullable: false),
                    FirewallDelay = table.Column<double>(type: "REAL", nullable: false),
                    FixedFirewall = table.Column<string>(type: "TEXT", nullable: true),
                    WorldKey = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_SystemModel_PersonModel_OwnerKey",
                        column: x => x.OwnerKey,
                        principalTable: "PersonModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SystemModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnownSystemModel",
                columns: table => new
                {
                    FromKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    Local = table.Column<bool>(type: "INTEGER", nullable: false),
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorldKey = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownSystemModel", x => new {x.FromKey, x.ToKey});
                    table.ForeignKey(
                        name: "FK_KnownSystemModel_SystemModel_FromKey",
                        column: x => x.FromKey,
                        principalTable: "SystemModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnownSystemModel_SystemModel_ToKey",
                        column: x => x.ToKey,
                        principalTable: "SystemModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnownSystemModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoginModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    SystemKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    Admin = table.Column<bool>(type: "INTEGER", nullable: false),
                    Person = table.Column<Guid>(type: "TEXT", nullable: false),
                    User = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Salt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    WorldKey = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_LoginModel_SystemModel_SystemKey",
                        column: x => x.SystemKey,
                        principalTable: "SystemModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoginModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VulnerabilityModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    SystemKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryPoint = table.Column<string>(type: "TEXT", nullable: false),
                    Exploits = table.Column<int>(type: "INTEGER", nullable: false),
                    Protocol = table.Column<string>(type: "TEXT", nullable: false),
                    Cve = table.Column<string>(type: "TEXT", nullable: true),
                    WorldKey = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VulnerabilityModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_VulnerabilityModel_SystemModel_SystemKey",
                        column: x => x.SystemKey,
                        principalTable: "SystemModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VulnerabilityModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileModel",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    SystemKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerKey = table.Column<Guid>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    Kind = table.Column<byte>(type: "INTEGER", nullable: false),
                    Read = table.Column<byte>(type: "INTEGER", nullable: false),
                    Write = table.Column<byte>(type: "INTEGER", nullable: false),
                    Execute = table.Column<byte>(type: "INTEGER", nullable: false),
                    Hidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    WorldKey = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileModel", x => x.Key);
                    table.ForeignKey(
                        name: "FK_FileModel_LoginModel_OwnerKey",
                        column: x => x.OwnerKey,
                        principalTable: "LoginModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileModel_SystemModel_SystemKey",
                        column: x => x.SystemKey,
                        principalTable: "SystemModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileModel_WorldModel_WorldKey",
                        column: x => x.WorldKey,
                        principalTable: "WorldModel",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_KnownSystemModel_ToKey",
                table: "KnownSystemModel",
                column: "ToKey");

            migrationBuilder.CreateIndex(
                name: "IX_KnownSystemModel_WorldKey",
                table: "KnownSystemModel",
                column: "WorldKey");

            migrationBuilder.CreateIndex(
                name: "IX_LoginModel_SystemKey",
                table: "LoginModel",
                column: "SystemKey");

            migrationBuilder.CreateIndex(
                name: "IX_LoginModel_WorldKey",
                table: "LoginModel",
                column: "WorldKey");

            migrationBuilder.CreateIndex(
                name: "IX_PersonModel_UserKey",
                table: "PersonModel",
                column: "UserKey");

            migrationBuilder.CreateIndex(
                name: "IX_PersonModel_WorldKey",
                table: "PersonModel",
                column: "WorldKey");

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

            migrationBuilder.CreateIndex(
                name: "IX_VulnerabilityModel_SystemKey",
                table: "VulnerabilityModel",
                column: "SystemKey");

            migrationBuilder.CreateIndex(
                name: "IX_VulnerabilityModel_WorldKey",
                table: "VulnerabilityModel",
                column: "WorldKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileModel");

            migrationBuilder.DropTable(
                name: "KnownSystemModel");

            migrationBuilder.DropTable(
                name: "RegistrationToken");

            migrationBuilder.DropTable(
                name: "VulnerabilityModel");

            migrationBuilder.DropTable(
                name: "LoginModel");

            migrationBuilder.DropTable(
                name: "SystemModel");

            migrationBuilder.DropTable(
                name: "PersonModel");

            migrationBuilder.DropTable(
                name: "UserModel");

            migrationBuilder.DropTable(
                name: "WorldModel");
        }
    }
}
