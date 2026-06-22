using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FijiPayroll.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffLoans",
                schema: "employee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LoanDescription = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalAmountToRepay = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DeductionAmountPerPeriod = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffLoans_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "company",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffLoanRepayments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<int>(type: "int", nullable: false),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RemainingBalanceAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffLoanRepayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffLoanRepayments_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffLoanRepayments_StaffLoans_LoanId",
                        column: x => x.LoanId,
                        principalSchema: "employee",
                        principalTable: "StaffLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffLoanRepayments_LoanId",
                schema: "payroll",
                table: "StaffLoanRepayments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffLoanRepayments_PayrollRunId",
                schema: "payroll",
                table: "StaffLoanRepayments",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffLoans_EmployeeId",
                schema: "employee",
                table: "StaffLoans",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffLoanRepayments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "StaffLoans",
                schema: "employee");
        }
    }
}
