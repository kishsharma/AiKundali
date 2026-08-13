using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KundaliEngine.Infrastructure.Migrations
{
    [DbContext(typeof(Data.KundaliDbContext))]
    [Migration("20260813093000_AddPortalNoteContextSnapshot")]
    public partial class AddPortalNoteContextSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextTitle",
                schema: "public",
                table: "PortalNotes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextDetailsJson",
                schema: "public",
                table: "PortalNotes",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContextTitle",
                schema: "public",
                table: "PortalNotes");

            migrationBuilder.DropColumn(
                name: "ContextDetailsJson",
                schema: "public",
                table: "PortalNotes");
        }
    }
}
