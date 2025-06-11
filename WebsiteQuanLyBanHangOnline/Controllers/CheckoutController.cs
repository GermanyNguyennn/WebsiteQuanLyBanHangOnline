using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using WebsiteQuanLyBanHangOnline.Areas.Admin.Repository;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;
using WebsiteQuanLyBanHangOnline.Services.Email;
using WebsiteQuanLyBanHangOnline.Services.MoMo;
using WebsiteQuanLyBanHangOnline.Services.VnPay;
namespace WebsiteQuanLyBanHangOnline.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IEmailSender _emailSender;
        private readonly IMoMoService _moMoService;
        private readonly IVnPayService _vnPayService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly EmailTemplateRenderer _emailRenderer;
        private readonly UserManager<AppUserModel> _userManager;
        public CheckoutController(DataContext context, IEmailSender emailSender, IMoMoService moMoService, IVnPayService vnPayService, IWebHostEnvironment webHostEnvironment, EmailTemplateRenderer emailTemplateRenderer, UserManager<AppUserModel> userManager)
        {
            _dataContext = context;
            _emailSender = emailSender;
            _moMoService = moMoService;
            _vnPayService = vnPayService;
            _webHostEnvironment = webHostEnvironment;
            _emailRenderer = emailTemplateRenderer;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Checkout(string PaymentMethod, string PaymentId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
                return RedirectToAction("Login", "Account");

            var orderCode = Guid.NewGuid().ToString();

            var orderItem = new OrderModel
            {
                OrderCode = orderCode,
                UserName = userEmail,
                PaymentMethod = $"{PaymentMethod} {PaymentId}",
                CreatedDate = DateTime.Now,
                Status = 1
            };

            _dataContext.Add(orderItem);

            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new List<CartModel>();
            var orderDetails = new List<OrderDetailModel>();
            var emailItems = new List<EmailOrderItemViewModel>();

            foreach (var item in cart)
            {
                var product = await _dataContext.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product == null || product.Quantity < item.Quantity)
                {
                    TempData["Error"] = $"Product with ID {item.ProductId} is not available in requested quantity.";
                    return RedirectToAction("Index", "Cart");
                }

                product.Quantity -= item.Quantity;
                product.Sold += item.Quantity;

                var detail = new OrderDetailModel
                {
                    OrderCode = orderCode,
                    UserName = userEmail,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                orderDetails.Add(detail);

                emailItems.Add(new EmailOrderItemViewModel
                {
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    Price = item.Price
                });
            }

            _dataContext.OrderDetails.AddRange(orderDetails);
            await _dataContext.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");

            var totalAmount = emailItems.Sum(i => i.Price * i.Quantity);

            // ViewModel gửi vào Razor View
            var viewModel = new EmailOrderViewModel
            {
                OrderCode = orderCode,
                UserName = userEmail,
                CreatedDate = DateTime.Now,
                Items = emailItems,
                TotalAmount = totalAmount
            };

            // Gửi email cho người mua
            var customerEmailHtml = await _emailRenderer.RenderAsync("CustomerEmail.cshtml", viewModel);
            await _emailSender.SendEmailAsync(userEmail, "Your Order Confirmation", customerEmailHtml);

            // Gửi email cho tất cả admin
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                var adminEmailHtml = await _emailRenderer.RenderAsync("AdminEmail.cshtml", viewModel);
                await _emailSender.SendEmailAsync(admin.Email, "New Order Received", adminEmailHtml);
            }

            TempData["Success"] = "Checkout Successfully!!!";
            return RedirectToAction("Index", "Home");
        }



        [HttpGet]
        public async Task<IActionResult> PaymentCallBackMoMo()
        {
            var query = HttpContext.Request.Query;
            var email = User.FindFirstValue(ClaimTypes.Email);
            var resultCode = query["resultCode"];
            var orderId = query["orderId"];

            var response = _moMoService.PaymentExecute(query);
            response.FullName = email;

            if (resultCode != "00")
            {
                _dataContext.Add(new MoMoModel
                {
                    OrderId = orderId,
                    FullName = email,
                    Amount = double.Parse(query["amount"]),
                    OrderInfo = query["orderInfo"],
                    CreatedDate = DateTime.Now
                });
                await _dataContext.SaveChangesAsync();

                await Checkout("MoMo", orderId);
            }
            else
            {
                TempData["error"] = "Checkout With MoMo Failed!!!";
                return RedirectToAction("Index", "Home");
            }

            return View(response);
        }


        [HttpGet]
        public async Task<IActionResult> PaymentCallBackVnPay()
        {
            var query = HttpContext.Request.Query;
            var response = _vnPayService.PaymentExecute(query);

            if (response.VnPayResponseCode == "00")
            {
                _dataContext.Add(new VnPayModel
                {
                    OrderId = response.OrderId,
                    PaymentMethod = response.PaymentMethod,
                    OrderDescription = response.OrderDescription,
                    TransactionId = response.TransactionId,
                    PaymentId = response.PaymentId,
                    CreatedDate = DateTime.Now
                });
                await _dataContext.SaveChangesAsync();

                await Checkout(response.PaymentMethod, response.OrderId);
            }
            else
            {
                TempData["error"] = "Checkout With VnPay Failed!!!";
                return RedirectToAction("Index", "Home");
            }

            return View(response);
        }
    }
}
