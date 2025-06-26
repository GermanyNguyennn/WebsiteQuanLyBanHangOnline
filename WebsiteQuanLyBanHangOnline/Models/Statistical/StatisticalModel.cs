using System.ComponentModel.DataAnnotations;

namespace WebsiteQuanLyBanHangOnline.Models.Statistical
{
    public class StatisticalModel
    {
        // Thông tin cơ bản về sản phẩm
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }

        // Số lượng và doanh thu - tổng cộng
        public int TotalQuantitySold { get; set; }                     // Tổng số lượng đã bán
        public decimal TotalRevenue { get; set; }                      // Tổng doanh thu
        public decimal TotalCost { get; set; }                         // Tổng giá vốn
        public decimal TotalProfit => TotalRevenue - TotalCost;        // Tổng lợi nhuận

        // Phân tách theo mã giảm giá
        public int QuantityWithCoupon { get; set; }                    // Số lượng bán có mã giảm giá
        public int QuantityWithoutCoupon { get; set; }                 // Số lượng bán không có mã giảm giá

        public decimal RevenueWithCoupon { get; set; }                 // Doanh thu từ đơn có mã giảm giá
        public decimal RevenueWithoutCoupon { get; set; }              // Doanh thu từ đơn không có mã giảm giá

        public decimal CostWithCoupon { get; set; }                    // Giá vốn đơn có mã giảm giá
        public decimal CostWithoutCoupon { get; set; }                 // Giá vốn đơn không có mã giảm giá

        public decimal ProfitWithCoupon => RevenueWithCoupon - CostWithCoupon;         // Lợi nhuận đơn có mã
        public decimal ProfitWithoutCoupon => RevenueWithoutCoupon - CostWithoutCoupon; // Lợi nhuận đơn không mã

        public decimal TotalDiscountCoupon { get; set; }               // Tổng tiền bị giảm do áp mã giảm giá
        public decimal LostProfitDueToDiscount => TotalDiscountCoupon;// Lợi nhuận mất đi do áp mã giảm giá (có thể chính là phần giảm giá)

        // Thời gian bán hàng
        public DateTime FirstSoldDate { get; set; }                    // Ngày bán đầu tiên
        public DateTime LastSoldDate { get; set; }                     // Ngày bán cuối cùng
    }
}
