using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebsiteQuanLyBanHangOnline.Migrations
{
    /// <inheritdoc />
    public partial class _15062025 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Coupons",
                newName: "DiscountValue");

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Coupons");

            migrationBuilder.RenameColumn(
                name: "DiscountValue",
                table: "Coupons",
                newName: "Price");
        }
    }
}
