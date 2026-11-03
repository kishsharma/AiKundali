using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KundaliEngine.Infrastructure.Migrations;

[DbContext(typeof(Data.KundaliDbContext))]
[Migration("20261102120000_AddAuthRefreshSessions")]
public partial class AddAuthRefreshSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuthRefreshSessions",
            schema: "public",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ReplacedByTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                RememberMe = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuthRefreshSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuthRefreshSessions_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "public",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuthRefreshSessions_TokenHash",
            schema: "public",
            table: "AuthRefreshSessions",
            column: "TokenHash",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AuthRefreshSessions_FamilyId_RevokedAtUtc",
            schema: "public",
            table: "AuthRefreshSessions",
            columns: new[] { "FamilyId", "RevokedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_AuthRefreshSessions_UserId_RevokedAtUtc",
            schema: "public",
            table: "AuthRefreshSessions",
            columns: new[] { "UserId", "RevokedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuthRefreshSessions", schema: "public");
    }
}
