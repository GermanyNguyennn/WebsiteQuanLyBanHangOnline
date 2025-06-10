using System.ComponentModel.DataAnnotations;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class MoMoModel
    {
        [Key]
        public int Id { get; set; }
        public string OrderId { get; set; }
        public string FullName { get; set; }
        public string OrderInfo { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
