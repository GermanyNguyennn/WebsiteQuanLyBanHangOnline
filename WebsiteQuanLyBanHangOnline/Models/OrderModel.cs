using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class OrderModel
    {
        [Key]
        public int Id { get; set; }

        public string OrderCode { get; set; }

        public string UserName { get; set; }

        public int? CouponId { get; set; }

        public string? CouponCode { get; set; }

        public DateTime CreatedDate { get; set; }

        public int Status { get; set; }

        public string? PaymentMethod { get; set; }

        [ForeignKey("CouponId")]
        public CouponModel? Coupon { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public string Address { get; set; }
        public string Ward { get; set; }
        public string District { get; set; }
        public string City { get; set; }
    }
}
