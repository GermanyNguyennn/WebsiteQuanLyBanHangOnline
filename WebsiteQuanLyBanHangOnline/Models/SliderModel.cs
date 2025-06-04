using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebsiteQuanLyBanHangOnline.Repository.Validation;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class SliderModel
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Image { get; set; }
        public string Description { get; set; }        
        public int Status { get; set; }
        [NotMapped]
        [FileExtention]
        public IFormFile? ImageUpload { get; set; }
    }
}
