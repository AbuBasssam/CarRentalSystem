using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFleetConditionStatusCondition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Cars_FleetConditionStatus",
                table: "Cars");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cars_FleetConditionStatus",
                table: "Cars",
                sql: "FleetConditionStatus>0 AND FleetConditionStatus<=4 ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Cars_FleetConditionStatus",
                table: "Cars");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cars_FleetConditionStatus",
                table: "Cars",
                sql: "FleetConditionStatus>0 AND FleetConditionStatus<=3 ");
        }
    }
}
