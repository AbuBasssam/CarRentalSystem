using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DecreaseCodeFieldSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Otps",
                type: "char(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(128)",
                oldMaxLength: 128);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Otps",
                type: "char(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(64)",
                oldMaxLength: 64);
        }
    }
}
