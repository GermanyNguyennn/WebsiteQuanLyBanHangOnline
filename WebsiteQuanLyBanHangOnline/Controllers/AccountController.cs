using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
                TempData["error"] = "Invalid Verification Code.";
                return View();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            TempData["success"] = "Two-Step Authentication Enabled.";

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
                    TempData["error"] = "Account Does Not Exist!!!";
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

                    TempData["success"] = "Account Login Successful!!!";
                    return Redirect(loginViewModel.ReturnURL ?? "/");
                }

                TempData["error"] = "Account Login Failed!!!";
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
                TempData["error"] = "Please Enter Verification Code";
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

                TempData["success"] = "Two-Step Verification Successful";
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout", "Account");
            }

            TempData["error"] = "Invalid Verification Code.";
            return View();
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
                        TempData["success"] = "Account Created Successfully!!!";
                    }    
                    else
                    {
                        TempData["error"] = "Account Creation Failed!!!";
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
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return NotFound();
            }

            var orderDetails = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderCode == orderCode)
                .ToListAsync();

            if (!orderDetails.Any())
            {
                return NotFound();
            }

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
                TempData["error"] = "Email Not Found!!!";
                return RedirectToAction("ForgotPassword");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action("NewPassword", "Account", new
            {
                email = user.Email,
                token = HttpUtility.UrlEncode(token)
            }, Request.Scheme);

            var message = $"Click <a href='{url}'>Here</a> To Reset Your Password.";

            await _emailSender.SendEmailAsync(user.Email, "Reset Your Password", message);

            TempData["success"] = "Password Reset Email Sent!";
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
                TempData["error"] = "Email Not Found!!!";
                return RedirectToAction("ForgotPassword");
            }

            var decodedToken = HttpUtility.UrlDecode(model.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["success"] = "Password Updated successfully!!!";
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
                return NotFound("User Not Found!!!");
            }

            var shipping = await _dataContext.Information.FirstOrDefaultAsync(s => s.UserId == userId);

            var model = new InformationViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = shipping?.Address,
                City = shipping?.City,
                District = shipping?.District,
                Ward = shipping?.Ward
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Information(InformationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound();

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
                    City = model.City
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
            TempData["success"] = "Update Information Successfully!!!";
            return RedirectToAction("Information", "Account");
        }
    }
}
