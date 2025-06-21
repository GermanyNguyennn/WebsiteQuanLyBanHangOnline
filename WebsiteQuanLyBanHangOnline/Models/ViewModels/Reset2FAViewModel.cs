using System.ComponentModel.DataAnnotations;

namespace WebsiteQuanLyBanHangOnline.Models.ViewModels
{
    public class Reset2FAViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
