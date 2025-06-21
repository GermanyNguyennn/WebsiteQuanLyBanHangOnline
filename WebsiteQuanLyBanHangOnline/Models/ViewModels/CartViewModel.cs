namespace WebsiteQuanLyBanHangOnline.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartModel> Cart { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public InformationViewModel Information { get; set; }
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAfterDiscount => TotalAmount - DiscountAmount;
    }
}
