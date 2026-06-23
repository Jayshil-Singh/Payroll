using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FijiPayroll.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTerminationDateAndSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TerminationDate",
                schema: "company",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DefaultPayFrequency = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    DefaultPayrollCalendar = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    NegativePayPolicy = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    DefaultSubmissionPaths = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    BackupDirectory = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    ExportDirectory = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    ImportDirectory = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    SmtpHost = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SmtpUsername = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    SmtpPassword = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    SmtpSslEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_CompanyId",
                schema: "company",
                table: "SystemSettings",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings",
                schema: "company");

            migrationBuilder.DropColumn(
                name: "TerminationDate",
                schema: "company",
                table: "Employees");
        }
    }
}
