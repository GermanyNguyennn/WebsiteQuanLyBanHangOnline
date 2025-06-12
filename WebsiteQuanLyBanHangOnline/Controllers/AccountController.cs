using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using WebsiteQuanLyBanHangOnline.Areas.Admin.Repository;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    public class AccountController : BaseController
    {
        private UserManager<AppUserModel> _userManager;
        private SignInManager<AppUserModel> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly DataContext _dataContext;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AccountController(UserManager<AppUserModel> userManager, SignInManager<AppUserModel> signInManager, IEmailSender emailSender, DataContext dataContext, RoleManager<IdentityRole> roleManager) : base(userManager, signInManager)
        {           
            _userManager = userManager;
            _signInManager = signInManager;
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
                TempData.Keep("UserId"); // Giữ lại nếu cần dùng tiếp
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
                ModelState.AddModelError("", "Mã xác thực không hợp lệ.");
                return View();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            TempData["Success"] = "Đã bật xác thực hai bước.";

            // Sau khi bật 2FA → tự đăng nhập lại nếu cần
            await _signInManager.SignInAsync(user, isPersistent: false);

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
                    ModelState.AddModelError("", "Account Does Not Exist!!!");
                    return View(loginViewModel);
                }

                var signInResult = await _signInManager.PasswordSignInAsync(user, loginViewModel.Password, false, false);

                if (signInResult.RequiresTwoFactor)
                {
                    // Lưu user ID tạm nếu cần sau đó
                    TempData["UserId"] = user.Id;
                    return RedirectToAction("Verify2FA");
                }

                if (signInResult.Succeeded)
                {
                    // Nếu là admin và chưa bật 2FA, chuyển sang Enable2FA
                    var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                    var has2FA = await _userManager.GetTwoFactorEnabledAsync(user);

                    if (isAdmin && !has2FA)
                    {
                        TempData["UserId"] = user.Id;
                        return RedirectToAction("Enable2FA", "Account");
                    }

                    TempData["Success"] = "Login Account Successfully!!!";
                    return Redirect(loginViewModel.ReturnURL ?? "/");
                }

                ModelState.AddModelError("", "Unable To Login!!!");
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
                ModelState.AddModelError("", "Vui lòng nhập mã xác thực.");
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
                // Lưu trạng thái đã xác thực 2FA vào session
                HttpContext.Session.SetString("Is2FACompleted", "true");

                // Nếu cần kiểm tra role admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                HttpContext.Session.SetString("IsAdmin", isAdmin ? "true" : "false");

                TempData["Success"] = "Xác thực hai bước thành công!";
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout", "Account");
            }

            ModelState.AddModelError("", "Mã xác thực không hợp lệ.");
            return View();
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        Microsoft.AspNetCore.Identity.SignInResult signInResult = await _signInManager.PasswordSignInAsync(loginViewModel.UserName, loginViewModel.Password, false, false);
        //        if (signInResult.Succeeded)
        //        {
        //            TempData["Success"] = "Login Account Successfully!!!";
        //            return Redirect(loginViewModel.ReturnURL ?? "/");
        //        }
        //        ModelState.AddModelError("", "Unable To Login!!!");
        //    }
        //    return View(loginViewModel);
        //}


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
                AppUserModel appUserModel = new AppUserModel { UserName = userModel.UserName, Email = userModel.Email, PhoneNumber = userModel.Phone };
                IdentityResult identityResult = await _userManager.CreateAsync(appUserModel, userModel.Password);
                if (identityResult.Succeeded)
                {
                    bool roleCustomer = await _roleManager.RoleExistsAsync("Customer");
                    if (!roleCustomer)
                    {
                        TempData["error"] = "Admin Has Not Added Customer Role!!!";
                        return View(userModel);
                    }                       
                    
                    var result = await _userManager.AddToRoleAsync(appUserModel, "Customer");
                    if (result.Succeeded)
                    {
                        TempData["success"] = "Create Account Successfully!!!";
                    }    
                    else
                    {
                        TempData["error"] = "Create Account Failed!!!";
                    }
                    return RedirectToAction("Login");
                }
                foreach (IdentityError error in identityResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }   
            }
            return View(userModel);
        }

        public async Task<IActionResult> Logout(string returnURL = "/")
        {
            await _signInManager.SignOutAsync();

            // XÓA TOÀN BỘ SESSION sau khi đăng xuất
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

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _dataContext.Orders.Where(od => od.UserName == userEmail).OrderByDescending(od => od.CreatedDate).ToListAsync();

            ViewBag.UserEmail = userEmail;
            return View(orders);
        }

        public async Task<IActionResult> View(string orderCode)
        {
            var orderDetail = await _dataContext.OrderDetails.Include(c => c.Product).Where(c => c.OrderCode == orderCode).ToListAsync();
            return View(orderDetail);
        }

        [HttpPost]
        public async Task<IActionResult> SendMailForgotPassword(AppUserModel appUserModel)
        {
            var checkMail = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == appUserModel.Email);

            if (checkMail == null)
            {
                TempData["error"] = "Email Not Found";
                return RedirectToAction("ForgotPassword", "Account");
            }
            else
            {
                string token = Guid.NewGuid().ToString();
                checkMail.Token = token;
                _dataContext.Update(checkMail);
                await _dataContext.SaveChangesAsync();
                var receiver = checkMail.Email;
                var subject = "Change password for user " + checkMail.Email;
                var message = "Click on link to recover password " + "<a href='" + $"{Request.Scheme}://{Request.Host}/Account/NewPassword?email=" + checkMail.Email + "&token=" + token + "'>";

                await _emailSender.SendEmailAsync(receiver, subject, message);
            }

            TempData["success"] = "An email has been sent to your registered email address with password reset instructions.";
            return RedirectToAction("ForgotPassword", "Account");
        }
        public IActionResult ForgotPassword()
        {
            return View();
        }
        public async Task<IActionResult> NewPassword(AppUserModel appUserModel, string token)
        {
            var checkuser = await _userManager.Users.Where(u => u.Email == appUserModel.Email).Where(u => u.Token == appUserModel.Token).FirstOrDefaultAsync();

            if (checkuser != null)
            {
                ViewBag.Email = checkuser.Email;
                ViewBag.Token = token;
            }
            else
            {
                TempData["error"] = "Email Not Found Or Token Not True";
                return RedirectToAction("ForgotPassword", "Account");
            }
            return View();
        }
        public async Task<IActionResult> UpdateNewPassword(AppUserModel appUserModel, string token)
        {
            var checkuser = await _userManager.Users.Where(u => u.Email == appUserModel.Email).Where(u => u.Token == appUserModel.Token).FirstOrDefaultAsync();

            if (checkuser != null)
            {
                string newtoken = Guid.NewGuid().ToString();
                var passwordHasher = new PasswordHasher<AppUserModel>();
                var passwordHash = passwordHasher.HashPassword(checkuser, appUserModel.PasswordHash);

                checkuser.PasswordHash = passwordHash;
                checkuser.Token = newtoken;

                var result = await _userManager.UpdateAsync(checkuser);
                if (result.Succeeded)
                {
                    TempData["success"] = "Update Password Successfully!!!";
                }
                else
                {
                    TempData["error"] = "Update Password Failed!!!";
                }    
                return RedirectToAction("Login", "Account");
            }
            else
            {
                TempData["error"] = "Email Not Found Or Token Not True";
                return RedirectToAction("ForgotPassword", "Account");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> InformationAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userById = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userById == null)
            {
                return NotFound();
            }

            var model = new AppUserModel
            {
                Id = userById.Id,
                UserName = userById.UserName,
                Email = userById.Email,
                PhoneNumber = userById.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Information(AppUserModel appUserModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userById = await _userManager.FindByIdAsync(userId);

            if (userById == null)
            {
                return NotFound();
            }

            userById.Email = appUserModel.Email;
            userById.PhoneNumber = appUserModel.PhoneNumber;

            var result = await _userManager.UpdateAsync(userById);
            if (result.Succeeded)
            {
                TempData["success"] = "Update Information Successfully!!!";
            }
            else
            {
                TempData["error"] = "Update Information Failed!!!";
            }

            return RedirectToAction("Information", "Account");
        }
    }
}
