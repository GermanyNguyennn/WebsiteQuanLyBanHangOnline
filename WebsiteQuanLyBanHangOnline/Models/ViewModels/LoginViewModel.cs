using System.ComponentModel.DataAnnotations;

namespace WebsiteQuanLyBanHangOnline.Models.ViewModels
{
    public class LoginViewModel
    {
        public string Id { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string ReturnURL { get; set; }
    }
}
