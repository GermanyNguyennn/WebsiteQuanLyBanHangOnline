using System.ComponentModel.DataAnnotations;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public enum DiscountType
    {
        Fixed = 0,
        Percent = 1
    }

    public class CouponModel
    {
        [Key]
        public int Id { get; set; }
        public string CouponCode { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Status { get; set; }
    }
}
