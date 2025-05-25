using Microsoft.AspNetCore.Identity;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class AppUserModel : IdentityUser
    {
        public string RoleId { get; set; }
        public IdentityRole Role { get; set; }
    }
}
