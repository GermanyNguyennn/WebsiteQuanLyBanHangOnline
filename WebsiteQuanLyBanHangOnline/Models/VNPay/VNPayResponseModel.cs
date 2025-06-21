namespace WebsiteQuanLyBanHangOnline.Models.VnPay
{
    public class VNPayResponseModel
    {
        public string OrderId { get; set; }
        public string OrderInfo { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentId { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public bool Success { get; set; }
        public string Token { get; set; }
        public string VnPayResponseCode { get; set; }
    }
}
