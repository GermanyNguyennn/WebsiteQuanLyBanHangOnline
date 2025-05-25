using System.Drawing.Printing;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class OrderDetailModel
    {
        public int Id { get; set; }
        public string OrderCode { get; set; }
        public string UserName { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public ProductModel Product { get; set; }

    }
}
