using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using WebsiteQuanLyBanHangOnline.Areas.Admin.Repository;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.MoMo;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Models.VnPay;
using WebsiteQuanLyBanHangOnline.Repository;
using WebsiteQuanLyBanHangOnline.Services.Email;
using WebsiteQuanLyBanHangOnline.Services.Location;
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
        private readonly ILocationService _locationService;

        public CheckoutController(DataContext context, IEmailSender emailSender, IMoMoService moMoService, IVnPayService vnPayService,
            IWebHostEnvironment webHostEnvironment, EmailTemplateRenderer emailTemplateRenderer,
            UserManager<AppUserModel> userManager, ILocationService locationService)
        {
            _dataContext = context;
            _emailSender = emailSender;
            _moMoService = moMoService;
            _vnPayService = vnPayService;
            _webHostEnvironment = webHostEnvironment;
            _emailRenderer = emailTemplateRenderer;
            _userManager = userManager;
            _locationService = locationService;
        }

        public IActionResult Index() => View();

        public async Task<IActionResult> Checkout(string PaymentMethod, string PaymentId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            if (userEmail == null) return RedirectToAction("Login", "Account");

            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new();
            var orderCode = Guid.NewGuid().ToString();
            var couponCode = HttpContext.Session.GetString("AppliedCoupon");
            var discountAmount = decimal.TryParse(HttpContext.Session.GetString("DiscountAmount"), out var parsedDiscount) ? parsedDiscount : 0;
            int? couponId = null;

            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = await _dataContext.Coupons.FirstOrDefaultAsync(c =>
                    c.CouponCode == couponCode && c.Status == 1 &&
                    c.Quantity > 0 && DateTime.Now >= c.StartDate && DateTime.Now <= c.EndDate);

                if (coupon != null)
                {
                    couponId = coupon.Id;
                    coupon.Quantity--;
                }
            }

            var user = await _userManager.Users.Include(u => u.Information).FirstOrDefaultAsync(u => u.UserName == userName);
            var info = user?.Information;

            var orderItem = new OrderModel
            {
                OrderCode = orderCode,
                UserName = userName,
                PaymentMethod = PaymentMethod == "COD" ? "COD" : $"{PaymentMethod} {PaymentId}",
                CreatedDate = DateTime.Now,
                Status = 1,
                CouponCode = couponCode,
                CouponId = couponId,
                FullName = user?.FullName,
                Email = user?.Email,
                PhoneNumber = user?.PhoneNumber,
                Address = info?.Address ?? "",
                City = await _locationService.GetCityNameById(info?.City ?? ""),
                District = await _locationService.GetDistrictNameById(info?.City ?? "", info?.District ?? ""),
                Ward = await _locationService.GetWardNameById(info?.District ?? "", info?.Ward ?? "")
            };

            _dataContext.Orders.Add(orderItem);
            await _dataContext.SaveChangesAsync();

            var orderDetails = new List<OrderDetailModel>();
            var emailItems = new List<EmailOrderItemViewModel>();
            decimal totalAmount = 0;

            foreach (var item in cart)
            {
                var product = await _dataContext.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product == null || product.Quantity < item.Quantity)
                {
                    TempData["error"] = $"Sản Phẩm '{item.ProductName}' Không Đủ Số Lượng.";
                    return RedirectToAction("Index", "Cart");
                }

                product.Quantity -= item.Quantity;
                product.Sold += item.Quantity;

                orderDetails.Add(new OrderDetailModel
                {
                    OrderId = orderItem.Id,
                    OrderCode = orderCode,
                    UserName = userName,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });

                emailItems.Add(new EmailOrderItemViewModel
                {
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    Price = item.Price
                });

                totalAmount += item.Price * item.Quantity;
            }

            _dataContext.OrderDetails.AddRange(orderDetails);
            await _dataContext.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("AppliedCoupon");
            HttpContext.Session.Remove("DiscountAmount");

            await SendOrderEmails(userEmail, userName, orderCode, emailItems, totalAmount, couponCode, discountAmount);

            TempData["success"] = "Thanh Toán Thành công!";
            return RedirectToAction("Index", "Home");
        }

        private async Task SendOrderEmails(string userEmail, string userName, string orderCode, List<EmailOrderItemViewModel> items,
            decimal totalAmount, string? couponCode, decimal discountAmount)
        {
            var viewModel = new EmailOrderViewModel
            {
                OrderCode = orderCode,
                UserName = userName,
                CreatedDate = DateTime.Now,
                Items = items,
                TotalAmount = totalAmount,
                CouponCode = couponCode,
                DiscountAmount = discountAmount
            };

            var customerHtml = await _emailRenderer.RenderAsync("CustomerEmail.cshtml", viewModel);
            await _emailSender.SendEmailAsync(userEmail, "Xác Nhận Đơn Hàng", customerHtml);

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                var adminHtml = await _emailRenderer.RenderAsync("AdminEmail.cshtml", viewModel);
                await _emailSender.SendEmailAsync(admin.Email, "Đơn Hàng Mới", adminHtml);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallBackMoMo()
        {
            var query = HttpContext.Request.Query;
            var resultCode = query["resultCode"];
            var orderId = query["orderId"];
            var orderInfo = query["orderInfo"];
            var amount = decimal.Parse(query["amount"]);

            _ = _moMoService.PaymentExecuteAsync(query);

            if (resultCode != "0")
            {
                var momoModel = new MoMoModel
                {
                    OrderId = orderId,
                    OrderInfo = orderInfo,
                    Amount = amount,
                    CreatedDate = DateTime.Now
                };

                _dataContext.Add(momoModel);
                await _dataContext.SaveChangesAsync();

                await Checkout("MoMo", orderId);

                return View(new MoMoInformationModel
                {
                    OrderId = orderId,
                    OrderInfo = orderInfo,
                    Amount = (double)amount,
                    CreatedDate = momoModel.CreatedDate
                });
            }

            TempData["error"] = "Thanh Toán Bằng MoMo Không Thành Công.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallBackVNPay()
        {
            var response = await _vnPayService.PaymentExecuteAsync(HttpContext.Request.Query);

            if (response.VnPayResponseCode == "00")
            {
                var vnPayModel = new VnPayModel
                {
                    OrderId = response.OrderId,
                    OrderInfo = response.OrderInfo,
                    Amount = response.Amount,
                    CreatedDate = DateTime.Now
                };

                _dataContext.Add(vnPayModel);
                await _dataContext.SaveChangesAsync();

                await Checkout(response.PaymentMethod, response.OrderId);

                return View(new VNPayInformationModel
                {
                    OrderId = response.OrderId,
                    OrderInfo = response.OrderInfo,
                    Amount = response.Amount,
                    CreatedDate = vnPayModel.CreatedDate
                });
            }

            TempData["error"] = "Thanh Toán Bằng VNPay Không Thành Công.";
            return RedirectToAction("Index", "Home");
        }
    }
}
