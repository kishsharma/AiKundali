using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KundaliEngine.Infrastructure.Migrations
{
    [DbContext(typeof(Data.KundaliDbContext))]
    [Migration("20260809123000_AddUserKundaliSaveLimit")]
    public partial class AddUserKundaliSaveLimit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KundaliSaveLimit",
                schema: "public",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 5);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KundaliSaveLimit",
                schema: "public",
                table: "Users");
        }
    }
}