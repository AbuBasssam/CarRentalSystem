using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridPricingPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CustomDailyRate",
                table: "Cars",
                type: "decimal(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomMonthlyRate",
                table: "Cars",
                type: "decimal(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomWeeklyRate",
                table: "Cars",
                type: "decimal(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyKmLimit",
                table: "CarCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomDailyRate",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "CustomMonthlyRate",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "CustomWeeklyRate",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "DailyKmLimit",
                table: "CarCategories");
        }
    }
}
