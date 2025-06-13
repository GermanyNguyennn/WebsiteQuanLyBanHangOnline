using System.ComponentModel.DataAnnotations;

namespace WebsiteQuanLyBanHangOnline.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }
}
