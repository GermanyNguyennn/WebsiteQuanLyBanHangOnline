namespace WebsiteQuanLyBanHangOnline.Models.Statistical
{
    public class StatisticalOrderDetailModel
    {
        public string OrderCode { get; set; }
        public string CouponCode { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public decimal PriceBefore { get; set; }
        public decimal PriceAfter { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal Profit { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
