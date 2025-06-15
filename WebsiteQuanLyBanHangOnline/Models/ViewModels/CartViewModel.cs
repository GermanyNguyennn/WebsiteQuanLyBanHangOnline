namespace WebsiteQuanLyBanHangOnline.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartModel> Cart { get; set; }
        public decimal GrandTotal { get; set; }
        public InformationViewModel Information { get; set; } = new();

        // Thêm thuộc tính để nhập mã giảm giá
        public string? CouponCode { get; set; }

        // Thêm thông tin giảm giá áp dụng (nếu có)
        public decimal DiscountAmount { get; set; }
        public decimal TotalAfterDiscount => GrandTotal - DiscountAmount;
    }
}
