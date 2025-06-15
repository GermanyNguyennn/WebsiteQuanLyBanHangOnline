using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebsiteQuanLyBanHangOnline.Migrations
{
    /// <inheritdoc />
    public partial class _14062025_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Statisticals");

            migrationBuilder.DropColumn(
                name: "Profit",
                table: "Statisticals");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Statisticals");

            migrationBuilder.RenameColumn(
                name: "Sold",
                table: "Statisticals",
                newName: "TotalQuantitySold");

            migrationBuilder.RenameColumn(
                name: "Revenue",
                table: "Statisticals",
                newName: "ProductId");

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstSoldDate",
                table: "Statisticals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Statisticals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSoldDate",
                table: "Statisticals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "Statisticals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "Statisticals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalRevenue",
                table: "Statisticals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstSoldDate",
                table: "Statisticals");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Statisticals");

            migrationBuilder.DropColumn(
                name: "LastSoldDate",
                table: "Statisticals");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "Statisticals");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "Statisticals");

            migrationBuilder.DropColumn(
                name: "TotalRevenue",
                table: "Statisticals");

            migrationBuilder.RenameColumn(
                name: "TotalQuantitySold",
                table: "Statisticals",
                newName: "Sold");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "Statisticals",
                newName: "Revenue");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Statisticals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Profit",
                table: "Statisticals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Statisticals",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
