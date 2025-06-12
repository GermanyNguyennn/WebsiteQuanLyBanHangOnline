using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebsiteQuanLyBanHangOnline.Models;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    public class BaseController : Controller
    {
        protected readonly UserManager<AppUserModel> _userManager;
        protected readonly SignInManager<AppUserModel> _signInManager;

        public BaseController(UserManager<AppUserModel> userManager, SignInManager<AppUserModel> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task Set2FAStatusAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var is2FAEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
                var isRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);

                ViewBag.Is2FACompleted = !is2FAEnabled || isRemembered;
                ViewBag.IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            }
            else
            {
                ViewBag.Is2FACompleted = false;
                ViewBag.IsAdmin = false;
            }
        }
    }
}
