using System.ComponentModel.DataAnnotations;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class UserModel
    {   
        public string Id { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, Phone]
        public string Phone { get; set; }

    }
}
