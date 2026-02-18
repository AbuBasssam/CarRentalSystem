using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FeeltTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    NameEN = table.Column<string>(type: "varchar(75)", unicode: false, maxLength: 75, nullable: false),
                    NameAR = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    CityEN = table.Column<string>(type: "varchar(75)", unicode: false, maxLength: 75, nullable: false),
                    CityAR = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RentalPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BufferHours = table.Column<int>(type: "int", nullable: false),
                    MinCancellationLeadTimeHours = table.Column<int>(type: "int", nullable: false),
                    CancellationPenaltyPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NoShowPenaltyDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    NameEN = table.Column<string>(type: "varchar(75)", unicode: false, maxLength: 75, nullable: false),
                    NameAR = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    IsModelSpecific = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BaseDailyRate = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    BaseWeeklyRate = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    BaseMonthlyRate = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    AllowDifferentDropOff = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PolicyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarCategories_RentalPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "RentalPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    KmMileage = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PlateNumberEN = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    PlateNumberAR = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    VIN = table.Column<string>(type: "char(17)", unicode: false, fixedLength: true, maxLength: 17, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumberOfSeats = table.Column<byte>(type: "tinyint", nullable: false),
                    NumberOfBags = table.Column<byte>(type: "tinyint", nullable: false),
                    EngineCapacity = table.Column<short>(type: "smallint", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    FuelType = table.Column<byte>(type: "tinyint", nullable: false),
                    TransmissionType = table.Column<int>(type: "int", nullable: false),
                    FleetConditionStatus = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CurrentBranchId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    PolicyOverrideId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Id);
                    table.CheckConstraint("CK_Cars_FuelType", "FuelType>0 AND FuelType<=4 ");
                    table.CheckConstraint("CK_Cars_Mileage", "KmMileage >= 0");
                    table.CheckConstraint("CK_Cars_PlateNumberAR", "PlateNumberAR LIKE N'[ء-ي] [ء-ي] [ء-ي] [0-9][0-9][0-9][0-9]'");
                    table.CheckConstraint("CK_Cars_PlateNumberEN", "PlateNumberEN LIKE '[A-Z][A-Z][A-Z] [0-9][0-9][0-9][0-9]'");
                    table.CheckConstraint("CK_Cars_VIN", "LEN(VIN) = 17 AND VIN NOT LIKE N'%[^A-HJ-NPR-Z0-9]%'");
                    table.ForeignKey(
                        name: "FK_Cars_Branches_CurrentBranchId",
                        column: x => x.CurrentBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cars_CarCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CarCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cars_RentalPolicies_PolicyOverrideId",
                        column: x => x.PolicyOverrideId,
                        principalTable: "RentalPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CarBranchHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    MovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CarId = table.Column<int>(type: "int", nullable: false),
                    FromBranchId = table.Column<int>(type: "int", nullable: false),
                    ToBranchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarBranchHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarBranchHistories_Branches_FromBranchId",
                        column: x => x.FromBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarBranchHistories_Branches_ToBranchId",
                        column: x => x.ToBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarBranchHistories_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ImageUrl = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: false),
                    CarId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarImages_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarBranchHistories_CarId_MovedAt",
                table: "CarBranchHistories",
                columns: new[] { "CarId", "MovedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CarBranchHistories_FromBranchId",
                table: "CarBranchHistories",
                column: "FromBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CarBranchHistories_ToBranchId",
                table: "CarBranchHistories",
                column: "ToBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CarCategories_PolicyId",
                table: "CarCategories",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_CarImages_CarId_IsPrimary",
                table: "CarImages",
                columns: new[] { "CarId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Brand",
                table: "Cars",
                column: "Brand");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_CategoryId",
                table: "Cars",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_CurrentBranchId_CategoryId",
                table: "Cars",
                columns: new[] { "CurrentBranchId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_FuelType",
                table: "Cars",
                column: "FuelType");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_IsActive_FleetConditionStatus",
                table: "Cars",
                columns: new[] { "IsActive", "FleetConditionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_PlateNumberAR",
                table: "Cars",
                column: "PlateNumberAR",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cars_PlateNumberEN",
                table: "Cars",
                column: "PlateNumberEN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cars_PolicyOverrideId",
                table: "Cars",
                column: "PolicyOverrideId");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_TransmissionType",
                table: "Cars",
                column: "TransmissionType");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_VIN",
                table: "Cars",
                column: "VIN",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarBranchHistories");

            migrationBuilder.DropTable(
                name: "CarImages");

            migrationBuilder.DropTable(
                name: "Cars");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "CarCategories");

            migrationBuilder.DropTable(
                name: "RentalPolicies");
        }
    }
}
