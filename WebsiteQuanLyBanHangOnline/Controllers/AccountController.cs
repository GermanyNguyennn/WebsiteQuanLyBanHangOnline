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

        [HttpGet]
        public async Task<IActionResult> Enable2FA()
        {
            AppUserModel user;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                user = await _userManager.GetUserAsync(User);
            }
            else if (TempData["UserId"] != null)
            {
                var userId = TempData["UserId"].ToString();
                user = await _userManager.FindByIdAsync(userId);
                TempData.Keep("UserId");
            }
            else
            {
                return RedirectToAction("Login");
            }

            if (user == null) return RedirectToAction("Login");

            var is2FAEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (is2FAEnabled) return RedirectToAction("Index", "Home");

            var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(authenticatorKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            string issuer = "WebsiteQuanLyBanHangOnline";
            string email = user.Email;

            var qrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
                            $"?secret={authenticatorKey}&issuer={Uri.EscapeDataString(issuer)}";

            ViewBag.QrCodeUrl = qrCodeUrl;
            ViewBag.Key = authenticatorKey;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Enable2FA(string verificationCode)
        {
            AppUserModel user;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                user = await _userManager.GetUserAsync(User);
            }
            else if (TempData["UserId"] != null)
            {
                var userId = TempData["UserId"].ToString();
                user = await _userManager.FindByIdAsync(userId);
            }
            else
            {
                return RedirectToAction("Login");
            }

            if (user == null) return RedirectToAction("Login");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                verificationCode);

            if (!isValid)
            {
                var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
                string issuer = "WebsiteQuanLyBanHangOnline";
                string email = user.Email;
                var qrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
                                $"?secret={authenticatorKey}&issuer={Uri.EscapeDataString(issuer)}";

                ViewBag.QrCodeUrl = qrCodeUrl;
                ViewBag.Key = authenticatorKey;
                TempData["error"] = "Mã Xác Thực Không Hợp Lệ.";
                return View();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            TempData["success"] = "Đã Bật Xác Thực 2 Bước.";

            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();

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
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(loginViewModel.UserName);
                if (user == null)
                {
                    TempData["error"] = "Tài Khoản Không Tồn Tại.";
                    return View(loginViewModel);
                }

                var signInResult = await _signInManager.PasswordSignInAsync(user, loginViewModel.Password, false, false);

                if (signInResult.RequiresTwoFactor)
                {
                    TempData["UserId"] = user.Id;
                    return RedirectToAction("Verify2FA");
                }

                if (signInResult.Succeeded)
                {
                    var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                    var has2FA = await _userManager.GetTwoFactorEnabledAsync(user);

                    if (isAdmin && !has2FA)
                    {
                        TempData["UserId"] = user.Id;
                        return RedirectToAction("Enable2FA", "Account");
                    }

                    TempData["success"] = "Đăng Nhập Thành Công.";
                    return Redirect(loginViewModel.ReturnURL ?? "/");
                }

                TempData["error"] = "Đăng Nhập Thất Bại";
            }

            return View(loginViewModel);
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
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                verificationCode, isPersistent: false, rememberClient: false);

            if (result.Succeeded)
            {
                HttpContext.Session.SetString("Is2FACompleted", "true");

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                HttpContext.Session.SetString("IsAdmin", isAdmin ? "true" : "false");

                TempData["success"] = "Xác Thực 2 Bước Thành Công.";
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout", "Account");
            }

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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsInRoleAsync(user, "Admin")))
            {
                TempData["error"] = "Email Không Tồn Tại.";
                return View();
            }

            var token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "Reset2FA");
            var resetLink = Url.Action("ConfirmReset2FA", "Account", new { userId = user.Id, token = token }, Request.Scheme);

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
            if (ModelState.IsValid)
            {
                bool roleCustomer = await _roleManager.RoleExistsAsync("Customer");
                if (!roleCustomer)
                {
                    TempData["error"] = "Tài Khoản Admin Chưa Tạo Vai Trò Customer.";
                    return View(userModel);
                }

                AppUserModel appUserModel = new AppUserModel
                {
                    UserName = userModel.UserName,
                    FullName = userModel.FullName,
                    Email = userModel.Email,
                    PhoneNumber = userModel.PhoneNumber
                };

                IdentityResult identityResult = await _userManager.CreateAsync(appUserModel, userModel.Password);
                if (identityResult.Succeeded)
                {
                    var result = await _userManager.AddToRoleAsync(appUserModel, "Customer");
                    if (result.Succeeded)
                    {
                        TempData["success"] = "Đăng Ký Thành Công.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        TempData["error"] = "Đăng Ký Thất Bại.";
                        await _userManager.DeleteAsync(appUserModel);
                    }
                }
                else
                {
                    foreach (IdentityError error in identityResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            return View(userModel);
        }


        public async Task<IActionResult> Logout(string returnURL = "/")
        {
            await _signInManager.SignOutAsync();

            HttpContext.Session.Clear();

            return Redirect(returnURL);
        }

        public async Task<IActionResult> History()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userName = User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _dataContext.Orders.Where(od => od.UserName == userName).OrderByDescending(od => od.CreatedDate).ToListAsync();

            ViewBag.UserEmail = userName;
            return View(orders);
        }

        public async Task<IActionResult> View(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return NotFound();
            }

            var orders = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (orders == null)
            {
                return NotFound();
            }

            var orderDetails = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderCode == orderCode)
                .ToListAsync();

            CouponModel? coupons = null;
            if (!string.IsNullOrEmpty(orders.CouponCode))
            {
                coupons = await _dataContext.Coupons
                    .FirstOrDefaultAsync(c => c.CouponCode == orders.CouponCode);
            }

            ViewBag.Order = orders;
            ViewBag.Coupon = coupons;

            return View(orderDetails);
        }


        [HttpPost]
        public async Task<IActionResult> SendMailForgotPassword(AppUserModel appUserModel)
        {
            if (!ModelState.IsValid)
            {
                return await View("ForgotPassword");
            }

            var user = await _userManager.FindByEmailAsync(appUserModel.Email);
            if (user == null)
            {
                TempData["error"] = "Email Không Hợp Lệ.";
                return RedirectToAction("ForgotPassword");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action("NewPassword", "Account", new
            {
                email = user.Email,
                token = HttpUtility.UrlEncode(token)
            }, Request.Scheme);

            var message = $"Ấn Vào <a href='{url}'>Đây</a> Để Đặt Lại Mật Khẩu.";

            await _emailSender.SendEmailAsync(user.Email, "Đặt Lại Mật Khẩu", message);

            TempData["success"] = "Email Đặt Lại Mật Khẩu Đã Được Gửi.";
            return RedirectToAction("ForgotPassword");
        }


        public IActionResult ForgotPassword()
        {
            return View();
        }
        
        public IActionResult NewPassword(string email, string token)
        {
            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = HttpUtility.UrlDecode(token)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNewPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("NewPassword", model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["error"] = "Email Không Hợp Lệ.";
                return RedirectToAction("ForgotPassword");
            }

            var decodedToken = HttpUtility.UrlDecode(model.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["success"] = "Cập Nhật Mật Khẩu Thành Công.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

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
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(user);

            var information = await _dataContext.Information.FirstOrDefaultAsync(s => s.UserId == userId);
            if (information == null)
            {
                information = new InformationModel
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Address = model.Address,
                    Ward = model.Ward,
                    District = model.District,
                    City = model.City,
                };
                _dataContext.Information.Add(information);
            }
            else
            {
                information.Address = model.Address;
                information.Ward = model.Ward;
                information.District = model.District;
                information.City = model.City;
                _dataContext.Information.Update(information);
            }

            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Cập Nhật Thông Tin Cá Nhân Thành Công.";
            return RedirectToAction("Information", "Account");
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
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["success"] = "Đổi Mật Khẩu Thành Công.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View();
        }

    }
}
