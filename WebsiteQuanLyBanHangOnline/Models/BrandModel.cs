using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class BrandModel
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }

        public string Slug { get; set; }
        public int Status { get; set; }
    }
}
