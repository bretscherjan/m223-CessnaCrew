using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CessnaCrew.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Users_PayerId",
                        column: x => x.PayerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Turns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InviteToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsTokenActive = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turns_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceSplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ShareAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsSettled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceSplits_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceSplits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrewMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RoleOnBoard = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TurnId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrewMembers_Turns_TurnId",
                        column: x => x.TurnId,
                        principalTable: "Turns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Logbooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TurnId = table.Column<int>(type: "integer", nullable: false),
                    IsHighSeaMode = table.Column<bool>(type: "boolean", nullable: false),
                    EngineHoursStart = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    EngineHoursEnd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logbooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Logbooks_Turns_TurnId",
                        column: x => x.TurnId,
                        principalTable: "Turns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SwapRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestedTurnId = table.Column<int>(type: "integer", nullable: false),
                    OfferedTurnId = table.Column<int>(type: "integer", nullable: true),
                    RequesterId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SwapRequests_Turns_OfferedTurnId",
                        column: x => x.OfferedTurnId,
                        principalTable: "Turns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SwapRequests_Turns_RequestedTurnId",
                        column: x => x.RequestedTurnId,
                        principalTable: "Turns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SwapRequests_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogbookRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LogbookId = table.Column<int>(type: "integer", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    WindForceBft = table.Column<int>(type: "integer", nullable: true),
                    WindDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AirPressureHpa = table.Column<int>(type: "integer", nullable: true),
                    SailConfiguration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogbookRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogbookRows_Logbooks_LogbookId",
                        column: x => x.LogbookId,
                        principalTable: "Logbooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogbookRows_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrewMembers_TurnId",
                table: "CrewMembers",
                column: "TurnId");

            migrationBuilder.CreateIndex(
                name: "IX_CrewMembers_UserId",
                table: "CrewMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PayerId",
                table: "Invoices",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSplits_InvoiceId",
                table: "InvoiceSplits",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSplits_UserId",
                table: "InvoiceSplits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LogbookRows_CreatedByUserId",
                table: "LogbookRows",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LogbookRows_LogbookId",
                table: "LogbookRows",
                column: "LogbookId");

            migrationBuilder.CreateIndex(
                name: "IX_Logbooks_TurnId",
                table: "Logbooks",
                column: "TurnId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_OfferedTurnId",
                table: "SwapRequests",
                column: "OfferedTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_RequestedTurnId",
                table: "SwapRequests",
                column: "RequestedTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_SwapRequests_RequesterId",
                table: "SwapRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_InviteToken",
                table: "Turns",
                column: "InviteToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turns_OwnerId",
                table: "Turns",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrewMembers");

            migrationBuilder.DropTable(
                name: "InvoiceSplits");

            migrationBuilder.DropTable(
                name: "LogbookRows");

            migrationBuilder.DropTable(
                name: "SwapRequests");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Logbooks");

            migrationBuilder.DropTable(
                name: "Turns");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
