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
    public class AccountController : Controller
    {
        private UserManager<AppUserModel> _userManager;
        private SignInManager<AppUserModel> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly DataContext _dataContext;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AccountController(UserManager<AppUserModel> userManager, SignInManager<AppUserModel> signInManager, IEmailSender emailSender, DataContext dataContext, RoleManager<IdentityRole> roleManager)
        {           
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _dataContext = dataContext;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
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
                Microsoft.AspNetCore.Identity.SignInResult signInResult = await _signInManager.PasswordSignInAsync(loginViewModel.UserName, loginViewModel.Password, false, false);
                if (signInResult.Succeeded)
                {
                    TempData["Success"] = "Login Account Successfully!!!";
                    return Redirect(loginViewModel.ReturnURL ?? "/");
                }
                ModelState.AddModelError("", "Unable To Login!!!");
            }
            return View(loginViewModel);
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
