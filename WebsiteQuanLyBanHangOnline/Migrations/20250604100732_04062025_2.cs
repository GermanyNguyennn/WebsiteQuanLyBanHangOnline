using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebsiteQuanLyBanHangOnline.Migrations
{
    /// <inheritdoc />
    public partial class _04062025_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShippingPrice",
                table: "Orders",
                newName: "ShippingCost");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShippingCost",
                table: "Orders",
                newName: "ShippingPrice");
        }
    }
}
