using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schedia.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hosts",
                columns: table => new
                {
                    HostId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    CalendarId = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hosts", x => x.HostId);
                });

            migrationBuilder.CreateTable(
                name: "LegalTexts",
                columns: table => new
                {
                    LegalTextId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Lang = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    VersionLabel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalTexts", x => x.LegalTextId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecipients",
                columns: table => new
                {
                    RecipientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipients", x => x.RecipientId);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    BookingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HostId = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ClientEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    ClientCompany = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ClientPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ClientReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GoogleEventId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GoogleMeetLink = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    LegalTextId = table.Column<long>(type: "bigint", nullable: false),
                    LegalAcceptedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    LegalAcceptedIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingId);
                    table.CheckConstraint("CK_Bookings_Duration", "[DurationMinutes] IN (30,45,60)");
                    table.CheckConstraint("CK_Bookings_Status", "[Status] IN ('booked','failed')");
                    table.ForeignKey(
                        name: "FK_Bookings_Hosts_HostId",
                        column: x => x.HostId,
                        principalTable: "Hosts",
                        principalColumn: "HostId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_LegalTexts_LegalTextId",
                        column: x => x.LegalTextId,
                        principalTable: "LegalTexts",
                        principalColumn: "LegalTextId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingAudit",
                columns: table => new
                {
                    BookingAuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingAudit", x => x.BookingAuditId);
                    table.ForeignKey(
                        name: "FK_BookingAudit_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingIdempotency",
                columns: table => new
                {
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingIdempotency", x => x.IdempotencyKey);
                    table.ForeignKey(
                        name: "FK_BookingIdempotency_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingAudit_BookingId",
                table: "BookingAudit",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingIdempotency_BookingId",
                table: "BookingIdempotency",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ClientEmail",
                table: "Bookings",
                column: "ClientEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_HostId_StartUtc",
                table: "Bookings",
                columns: new[] { "HostId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_LegalTextId",
                table: "Bookings",
                column: "LegalTextId");

            migrationBuilder.CreateIndex(
                name: "IX_Hosts_Email",
                table: "Hosts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalTexts_Lang",
                table: "LegalTexts",
                column: "Lang",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_Email",
                table: "NotificationRecipients",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingAudit");

            migrationBuilder.DropTable(
                name: "BookingIdempotency");

            migrationBuilder.DropTable(
                name: "NotificationRecipients");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Hosts");

            migrationBuilder.DropTable(
                name: "LegalTexts");
        }
    }
}
