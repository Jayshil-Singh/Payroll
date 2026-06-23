using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FijiPayroll.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase17Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                schema: "company",
                table: "UserAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordUpdatedAt",
                schema: "company",
                table: "UserAccounts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "RetroactiveAdjustments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ComponentType = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AppliedInPayrollRunId = table.Column<int>(type: "int", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetroactiveAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPasswordHistories",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserAccountId = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPasswordHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPasswordHistories_UserAccounts_UserAccountId",
                        column: x => x.UserAccountId,
                        principalSchema: "company",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetroactiveAdjustments_CompanyId_EmployeeId",
                schema: "payroll",
                table: "RetroactiveAdjustments",
                columns: new[] { "CompanyId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_RetroactiveAdjustments_IsApplied",
                schema: "payroll",
                table: "RetroactiveAdjustments",
                column: "IsApplied");

            migrationBuilder.CreateIndex(
                name: "IX_UserPasswordHistories_UserAccountId",
                schema: "company",
                table: "UserPasswordHistories",
                column: "UserAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetroactiveAdjustments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "UserPasswordHistories",
                schema: "company");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                schema: "company",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "PasswordUpdatedAt",
                schema: "company",
                table: "UserAccounts");
        }
    }
}
