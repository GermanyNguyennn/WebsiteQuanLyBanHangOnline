namespace WebsiteQuanLyBanHangOnline.Models.ViewModels
{
    public class EmailOrderViewModel
    {
        public string UserName { get; set; }
        public string OrderCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<EmailOrderItemViewModel> Items { get; set; }
        public decimal TotalAmount { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalAfterDiscount => TotalAmount - DiscountAmount;
    }
}
