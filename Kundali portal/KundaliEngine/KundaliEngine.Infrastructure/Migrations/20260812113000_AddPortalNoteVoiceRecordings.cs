using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KundaliEngine.Infrastructure.Migrations
{
    [DbContext(typeof(Data.KundaliDbContext))]
    [Migration("20260812113000_AddPortalNoteVoiceRecordings")]
    public partial class AddPortalNoteVoiceRecordings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortalNoteVoiceRecordings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContextId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalNoteVoiceRecordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortalNoteVoiceRecordings_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortalNoteVoiceRecordings_UserId_ContextType_ContextId",
                schema: "public",
                table: "PortalNoteVoiceRecordings",
                columns: new[] { "UserId", "ContextType", "ContextId" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalNoteVoiceRecordings_CreatedAt",
                schema: "public",
                table: "PortalNoteVoiceRecordings",
                column: "CreatedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortalNoteVoiceRecordings",
                schema: "public");
        }
    }
}
