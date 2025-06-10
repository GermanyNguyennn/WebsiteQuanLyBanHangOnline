using WebsiteQuanLyBanHangOnline.Repository.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class ProductModel
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Image { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Sold { get; set; }
        public string Slug { get; set; }
        public int BrandId { get; set; }
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public CategoryModel Category { get; set; }
        [ForeignKey("BrandId")]
        public BrandModel Brand { get; set; }
        [NotMapped]
        [FileExtention]
        public IFormFile? ImageUpload { get; set; }
    }
}
