using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class ProductQuantityModel
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public int Quantity { get; set; }
        public int ProductId { get; set; }
        public DateTime CreatedDate { get; set; }
        [ForeignKey("ProductId")]

        public ProductModel Product { get; set; }
    }
}
