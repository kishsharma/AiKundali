using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KundaliEngine.Infrastructure.Migrations;

[DbContext(typeof(Data.KundaliDbContext))]
[Migration("20261031100000_AddPractitionerCrm")]
public partial class AddPractitionerCrm : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PractitionerCrmSchemaSql.Initial);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Kundalis_PractitionerClients_ClientId", schema: "public", table: "Kundalis");
        migrationBuilder.DropTable(name: "AppNotifications", schema: "public");
        migrationBuilder.DropTable(name: "PractitionerAppointments", schema: "public");
        migrationBuilder.DropTable(name: "PractitionerClients", schema: "public");
        migrationBuilder.DropIndex(name: "IX_Kundalis_ClientId", schema: "public", table: "Kundalis");
        migrationBuilder.DropColumn(name: "ClientId", schema: "public", table: "Kundalis");
    }
}
