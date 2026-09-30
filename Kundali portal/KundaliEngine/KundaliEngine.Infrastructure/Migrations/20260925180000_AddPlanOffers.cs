using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KundaliEngine.Infrastructure.Migrations
{
    [DbContext(typeof(Data.KundaliDbContext))]
    [Migration("20260925180000_AddPlanOffers")]
    public partial class AddPlanOffers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CouponOfferId",
                schema: "public",
                table: "UserSubscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                schema: "public",
                table: "UserSubscriptions",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingPlanConfigs",
                schema: "public",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Tagline = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MonthlyAmountPaise = table.Column<int>(type: "integer", nullable: false),
                    YearlyAmountPaise = table.Column<int>(type: "integer", nullable: false),
                    KundaliSaveLimit = table.Column<int>(type: "integer", nullable: false),
                    MatchSaveLimit = table.Column<int>(type: "integer", nullable: false),
                    CheckoutOpen = table.Column<bool>(type: "boolean", nullable: false),
                    RazorpayMonthlyPlanId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    RazorpayYearlyPlanId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPlanConfigs", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "CouponOffers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PercentOff = table.Column<int>(type: "integer", nullable: false),
                    PlanCodes = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    BillingCycles = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    RedeemedCount = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CouponRedemptions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CouponOfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RazorpayOrderId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PlanCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BillingCycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PercentOff = table.Column<int>(type: "integer", nullable: false),
                    AmountPaise = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponRedemptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CouponOffers_Code",
                schema: "public",
                table: "CouponOffers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemptions_CouponOfferId",
                schema: "public",
                table: "CouponRedemptions",
                column: "CouponOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemptions_RazorpayOrderId",
                schema: "public",
                table: "CouponRedemptions",
                column: "RazorpayOrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CouponRedemptions", schema: "public");
            migrationBuilder.DropTable(name: "CouponOffers", schema: "public");
            migrationBuilder.DropTable(name: "BillingPlanConfigs", schema: "public");
            migrationBuilder.DropColumn(name: "CouponOfferId", schema: "public", table: "UserSubscriptions");
            migrationBuilder.DropColumn(name: "CouponCode", schema: "public", table: "UserSubscriptions");
        }
    }
}
