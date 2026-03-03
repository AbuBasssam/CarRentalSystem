using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCarManagementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarCategories_RentalPolicies_PolicyId",
                table: "CarCategories");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "CarImages");

            migrationBuilder.DropColumn(
                name: "AllowDifferentDropOff",
                table: "CarCategories");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "RentalPolicies",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "RentalPolicies",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<decimal>(
                name: "CancellationPenaltyPercent",
                table: "RentalPolicies",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<bool>(
                name: "AllowDifferentDropOff",
                table: "RentalPolicies",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CarImages",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CarImages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "CarImages",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CarImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CarImages_IsDeleted_DeletedAt",
                table: "CarImages",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_CarCategories_RentalPolicies_PolicyId",
                table: "CarCategories",
                column: "PolicyId",
                principalTable: "RentalPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarCategories_RentalPolicies_PolicyId",
                table: "CarCategories");

            migrationBuilder.DropIndex(
                name: "IX_CarImages_IsDeleted_DeletedAt",
                table: "CarImages");

            migrationBuilder.DropColumn(
                name: "AllowDifferentDropOff",
                table: "RentalPolicies");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CarImages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CarImages");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "CarImages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CarImages");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "RentalPolicies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "RentalPolicies",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CancellationPenaltyPercent",
                table: "RentalPolicies",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "CarImages",
                type: "nvarchar(350)",
                maxLength: 350,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AllowDifferentDropOff",
                table: "CarCategories",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CarCategories_RentalPolicies_PolicyId",
                table: "CarCategories",
                column: "PolicyId",
                principalTable: "RentalPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
