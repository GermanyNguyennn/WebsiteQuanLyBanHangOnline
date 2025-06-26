using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web;
using WebsiteQuanLyBanHangOnline.Areas.Admin.Repository;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IEmailSender _emailSender;
        private readonly DataContext _dataContext;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AccountController(UserManager<AppUserModel> userManager, SignInManager<AppUserModel> signInManager, IEmailSender emailSender, DataContext dataContext, RoleManager<IdentityRole> roleManager) : base(userManager, signInManager)
        {           
            _emailSender = emailSender;
            _dataContext = dataContext;
            _roleManager = roleManager;
        }

        private async Task<AppUserModel?> GetUserFromContextAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return await _userManager.GetUserAsync(User);

            if (TempData["UserId"] != null)
            {
                var userId = TempData["UserId"].ToString();
                TempData.Keep("UserId");
                return await _userManager.FindByIdAsync(userId);
            }

            return null;
        }

        private async Task<string> EnsureAuthenticatorKeyAsync(AppUserModel user)
        {
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }
            return key;
        }

        private string GenerateQrCodeUrl(string email, string key, string userName)
        {
            string issuer = $"{userName}";
            return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
                   $"?secret={key}&issuer={Uri.EscapeDataString(issuer)}";
        }



        [HttpGet]
        public async Task<IActionResult> Enable2FA()
        {
            var user = await GetUserFromContextAsync();
            if (user == null) return RedirectToAction("Login");

            if (await _userManager.GetTwoFactorEnabledAsync(user))
                return RedirectToAction("Index", "Home");

            var key = await EnsureAuthenticatorKeyAsync(user);
            ViewBag.QrCodeUrl = GenerateQrCodeUrl(user.Email, key, user.UserName);
            ViewBag.Key = key;
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> Enable2FA(string verificationCode)
        {
            var user = await GetUserFromContextAsync();
            if (user == null) return RedirectToAction("Login");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, verificationCode);
            if (!isValid)
            {
                var key = await EnsureAuthenticatorKeyAsync(user);
                ViewBag.QrCodeUrl = GenerateQrCodeUrl(user.Email, key, user.UserName);
                ViewBag.Key = key;
                TempData["error"] = "Mã Xác Thực Không Hợp Lệ.";
                return View();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            TempData["success"] = "Đã Bật Xác Thực 2 Bước.";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Index()
        {
            await Set2FAStatusAsync();
            return View();
        }

        [HttpGet]
        public IActionResult Login(string returnURL)
        {
            return View(new LoginViewModel { ReturnURL = returnURL});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                TempData["error"] = "Tài khoản không tồn tại.";
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.RequiresTwoFactor)
            {
                TempData["UserId"] = user.Id;
                return RedirectToAction("Verify2FA", new { returnUrl = model.ReturnURL });
            }

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin") &&
                    !await _userManager.GetTwoFactorEnabledAsync(user))
                {
                    TempData["UserId"] = user.Id;
                    return RedirectToAction("Enable2FA", new { returnUrl = model.ReturnURL });
                }

                TempData["success"] = "Đăng nhập thành công.";

                if (!string.IsNullOrEmpty(model.ReturnURL) && Url.IsLocalUrl(model.ReturnURL))
                    return Redirect(model.ReturnURL);

                return RedirectToAction("Index", "Home");
            }

            TempData["error"] = "Đăng nhập thất bại. Kiểm tra lại thông tin.";
            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> Verify2FA()
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify2FA(string verificationCode)
        {
            if (string.IsNullOrWhiteSpace(verificationCode))
            {
                TempData["error"] = "Vui Lòng Nhập Mã Xác Thực.";
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToAction("Login");

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(verificationCode, false, false);

            if (result.Succeeded)
            {
                HttpContext.Session.SetString("Is2FACompleted", "true");
                HttpContext.Session.SetString("IsAdmin", (await _userManager.IsInRoleAsync(user, "Admin")) ? "true" : "false");
                TempData["success"] = "Đăng Nhập Thành Công.";
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut) return RedirectToAction("Lockout", "Account");

            TempData["error"] = "Mã Xác Thực Không Hợp Lệ.";
            return View();
        }


        [HttpGet]
        public IActionResult Reset2FA()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset2FA(Reset2FAViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["error"] = "Email Không Tồn Tại.";
                return View();
            }

            var token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "Reset2FA");
            var resetLink = Url.Action("ConfirmReset2FA", "Account", new { userId = user.Id, token }, Request.Scheme);

            await _emailSender.SendEmailAsync(model.Email, "Reset 2FA", $"Ấn Vào <a href='{resetLink}'>Đây</a> Để Đặt Lại Xác Thực 2 Bước.");
            TempData["success"] = "Đã Gửi Link Reset 2FA Đến Email Của Bạn.";
            return RedirectToAction("Login", "Account");
        }


        [HttpGet]
        public async Task<IActionResult> ConfirmReset2FA(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "Reset2FA", token);
            if (!isValid)
            {
                TempData["error"] = "Liên Kết Không Hợp Lệ Hoặc Đã Hết Hạn.";
                return RedirectToAction("Login", "Account");
            }

            var disableResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disableResult.Succeeded)
            {
                TempData["error"] = "Không Thể Reset 2FA.";
                return RedirectToAction("Login", "Account");
            }

            TempData["success"] = "Đã Reset 2FA. Vui Lòng Đăng Nhập Lại Để Thiết Lập 2FA.";
            return RedirectToAction("Login", "Account");
        }



        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(UserModel userModel)
        {
            if (!ModelState.IsValid) return View(userModel);

            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                TempData["error"] = "Admin Chưa Tạo Vai Trò 'Customer'.";
                return View(userModel);
            }

            var newUser = new AppUserModel
            {
                UserName = userModel.UserName,
                FullName = userModel.FullName,
                Email = userModel.Email,
                PhoneNumber = userModel.PhoneNumber
            };

            var result = await _userManager.CreateAsync(newUser, userModel.Password);
            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(newUser, "Customer");
                if (roleResult.Succeeded)
                {
                    TempData["success"] = "Đăng Ký Thành Công.";
                    return RedirectToAction("Login");
                }

                TempData["error"] = "Gán Vai Trò Thất Bại.";
                await _userManager.DeleteAsync(newUser);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(userModel);
        }



        public async Task<IActionResult> Logout(string returnURL = "/")
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            TempData["success"] = "Đăng Xuất Thành Công.";
            return Redirect(returnURL);
        }

        public async Task<IActionResult> History(int page = 1)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            const int pageSize = 10;
            if (page < 1) page = 1;

            var userName = User.FindFirstValue(ClaimTypes.Name);
            var totalOrders = await _dataContext.Orders
                .Where(o => o.UserName == userName)
                .CountAsync();

            var pager = new Paginate(totalOrders, page, pageSize);

            var orders = await _dataContext.Orders
                .Where(o => o.UserName == userName)
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            ViewBag.UserEmail = userName;

            return View(orders);
        }


        [HttpGet]
        public async Task<IActionResult> View(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return NotFound();

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null)
                return NotFound();

            var orderDetails = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderCode == orderCode)
                .ToListAsync();

            var coupon = string.IsNullOrEmpty(order.CouponCode)
                ? null
                : await _dataContext.Coupons.FirstOrDefaultAsync(c => c.CouponCode == order.CouponCode);

            ViewBag.Order = order;
            ViewBag.Coupon = coupon;
            return View(orderDetails);
        }


        [HttpPost]
        public async Task<IActionResult> SendMailForgotPassword(AppUserModel appUserModel)
        {
            if (!ModelState.IsValid)
                return await View("ForgotPassword");

            var user = await _userManager.FindByEmailAsync(appUserModel.Email);
            if (user == null)
            {
                TempData["error"] = "Email Không Hợp Lệ.";
                return RedirectToAction("ForgotPassword");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("NewPassword", "Account", new
            {
                email = user.Email,
                token = HttpUtility.UrlEncode(token)
            }, Request.Scheme);

            var body = $"Ấn Vào <a href='{callbackUrl}'>Đây</a> Để Đặt Lại Mật Khẩu.";

            await _emailSender.SendEmailAsync(user.Email, "Đặt Lại Mật Khẩu", body);

            TempData["success"] = "Email Đặt Lại Mật Khẩu Đã Được Gửi.";
            return RedirectToAction("ForgotPassword");
        }


        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        public IActionResult NewPassword(string email, string token)
        {
            return View(new ResetPasswordViewModel
            {
                Email = email,
                Token = HttpUtility.UrlDecode(token)
            });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateNewPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View("NewPassword", model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["error"] = "Email Không Hợp Lệ.";
                return RedirectToAction("ForgotPassword");
            }

            var result = await _userManager.ResetPasswordAsync(user, HttpUtility.UrlDecode(model.Token), model.NewPassword);

            if (result.Succeeded)
            {
                TempData["success"] = "Cập Nhật Mật Khẩu Thành Công.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View("NewPassword", model);
        }



        [HttpGet]
        public async Task<IActionResult> Information()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Không Tìm Thấy Thông Tin Cá Nhân.");
            }

            var information = await _dataContext.Information.FirstOrDefaultAsync(s => s.UserId == userId);

            var model = new InformationViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = information?.Address,
                City = information?.City,
                District = information?.District,
                Ward = information?.Ward
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Information(InformationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(user);

            var info = await _dataContext.Information.FirstOrDefaultAsync(i => i.UserId == userId);
            if (info == null)
            {
                info = new InformationModel
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Address = model.Address,
                    Ward = model.Ward,
                    District = model.District,
                    City = model.City
                };
                _dataContext.Information.Add(info);
            }
            else
            {
                info.Address = model.Address;
                info.Ward = model.Ward;
                info.District = model.District;
                info.City = model.City;
                _dataContext.Information.Update(info);
            }

            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Cập Nhật Thông Tin Cá Nhân Thành Công.";
            return RedirectToAction("Information");
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["error"] = "Vui Lòng Nhập Đầy Đủ Thông Tin.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["error"] = "Mật Khẩu Không Trùng Khớp.";
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["success"] = "Đổi Mật Khẩu Thành Công.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }

    }
}
