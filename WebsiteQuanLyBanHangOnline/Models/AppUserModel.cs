using Microsoft.AspNetCore.Identity;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class AppUserModel : IdentityUser
    {
        public string Token { get; set; }
    }
}
