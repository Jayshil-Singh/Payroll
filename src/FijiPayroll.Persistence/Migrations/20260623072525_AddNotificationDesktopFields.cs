using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FijiPayroll.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDesktopFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "payroll",
                table: "Notifications",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "Info");

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                schema: "payroll",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                schema: "payroll",
                table: "Notifications",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "payroll",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsRead",
                schema: "payroll",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                schema: "payroll",
                table: "Notifications");
        }
    }
}
