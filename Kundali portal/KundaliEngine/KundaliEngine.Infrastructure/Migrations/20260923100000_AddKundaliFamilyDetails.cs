using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KundaliEngine.Infrastructure.Migrations
{
    [DbContext(typeof(Data.KundaliDbContext))]
    [Migration("20260923100000_AddKundaliFamilyDetails")]
    public partial class AddKundaliFamilyDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Surname",
                schema: "public",
                table: "Kundalis",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherName",
                schema: "public",
                table: "Kundalis",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherName",
                schema: "public",
                table: "Kundalis",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gotra",
                schema: "public",
                table: "Kundalis",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gotra",
                schema: "public",
                table: "Kundalis");

            migrationBuilder.DropColumn(
                name: "MotherName",
                schema: "public",
                table: "Kundalis");

            migrationBuilder.DropColumn(
                name: "FatherName",
                schema: "public",
                table: "Kundalis");

            migrationBuilder.DropColumn(
                name: "Surname",
                schema: "public",
                table: "Kundalis");
        }
    }
}
