using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebsiteQuanLyBanHangOnline.Repository.Validation;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class ContactModel
    {
        [Key]
        [Required]
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogoImage {  get; set; }
        public string Description { get; set; }
        public string Map {  get; set; }
        public string Email { get; set; }
        public string Phone {  get; set; }
        [NotMapped]
        [FileExtention]
        public IFormFile? ImageUpload { get; set; }

    }
}
