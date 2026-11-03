using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KundaliEngine.Infrastructure.Migrations;

[DbContext(typeof(Data.KundaliDbContext))]
[Migration("20261101120000_ExpandPractitionerCrm")]
public partial class ExpandPractitionerCrm : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PractitionerCrmSchemaSql.Expansion);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_PractitionerAppointments_UserId_Status_CompletedAtUtc", schema: "public", table: "PractitionerAppointments");
        migrationBuilder.Sql("ALTER TABLE public.\"PractitionerAppointments\" DROP COLUMN IF EXISTS \"CompletedAtUtc\";");
        migrationBuilder.DropColumn(name: "Topic", schema: "public", table: "PractitionerAppointments");
        migrationBuilder.DropColumn(name: "Notes", schema: "public", table: "PractitionerClients");
        migrationBuilder.DropColumn(name: "Status", schema: "public", table: "PractitionerClients");
        migrationBuilder.DropColumn(name: "Topic", schema: "public", table: "PractitionerClients");
    }
}
