using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "TransmissionType",
                table: "Cars",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cars_FleetConditionStatus",
                table: "Cars",
                sql: "FleetConditionStatus>0 AND FleetConditionStatus<=3 ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cars_TransmissionType",
                table: "Cars",
                sql: "TransmissionType>0 AND TransmissionType<=2 ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Cars_FleetConditionStatus",
                table: "Cars");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Cars_TransmissionType",
                table: "Cars");

            migrationBuilder.AlterColumn<int>(
                name: "TransmissionType",
                table: "Cars",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");
        }
    }
}
