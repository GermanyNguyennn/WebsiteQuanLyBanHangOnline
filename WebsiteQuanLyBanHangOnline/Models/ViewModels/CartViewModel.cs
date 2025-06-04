namespace WebsiteQuanLyBanHangOnline.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartModel> Cart { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal ShippingPrice { get; set; }
    }
}
