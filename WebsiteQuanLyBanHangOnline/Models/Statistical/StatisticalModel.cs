using System.ComponentModel.DataAnnotations;

namespace WebsiteQuanLyBanHangOnline.Models.Statistical
{
    public class StatisticalModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }

        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }      // Doanh thu = Price * Quantity
        public decimal TotalCost { get; set; }         // Vốn = ImportPrice * Quantity
        public decimal TotalProfit => TotalRevenue - TotalCost;

        public DateTime FirstSoldDate { get; set; }
        public DateTime LastSoldDate { get; set; }
    }
}
