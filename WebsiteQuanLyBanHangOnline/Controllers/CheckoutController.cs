using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using WebsiteQuanLyBanHangOnline.Areas.Admin.Repository;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;
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
        public CheckoutController(DataContext context, IEmailSender emailSender, IMoMoService moMoService, IVnPayService vnPayService)
        {
            _dataContext = context;
            _emailSender = emailSender;
            _moMoService = moMoService;
            _vnPayService = vnPayService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Checkout(string PaymentMethod, string PaymentId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                var orderCode = Guid.NewGuid().ToString();
                var orderItem = new OrderModel();
                orderItem.OrderCode = orderCode;
                orderItem.UserName = userEmail;
                orderItem.PaymentMethod = PaymentMethod + " " + PaymentId;
                orderItem.CreatedDate = DateTime.Now;
                orderItem.Status = 1;
   
                _dataContext.Add(orderItem);
                _dataContext.SaveChanges();
                List<CartModel> cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new List<CartModel>();
                foreach (var cartItem in cart)
                {
                    var orderDetail = new OrderDetailViewModel();
                    orderDetail.UserName = userEmail;
                    orderDetail.OrderCode = orderCode;
                    orderDetail.ProductId = cartItem.ProductId;
                    orderDetail.Quantity = cartItem.Quantity;
                    orderDetail.Price = cartItem.Price;
                    orderDetail.Quantity = cartItem.Quantity;

                    var products = await _dataContext.Products.Where(p => p.Id == cartItem.ProductId).FirstAsync();
                    products.Quantity -= cartItem.Quantity;
                    products.Sold += cartItem.Quantity;

                    _dataContext.Update(products);
                    _dataContext.Add(orderDetail);
                    _dataContext.SaveChanges();
                    
                }
                HttpContext.Session.Remove("Cart");
                var receiver = userEmail;
                var subject = "Checkout Successfully!!!";
                var message = "Checkout Successfully!!! Enjoy The Momment <3";
                await _emailSender.SendEmailAsync(receiver, subject, message);
                TempData["Success"] = "Checkout Successfully!!!";
                
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallBackMoMo()
        {
            var requestQuery = HttpContext.Request.Query;
            var response = _moMoService.PaymentExecute(requestQuery);
            response.FullName = User.FindFirstValue(ClaimTypes.Email);

            if (requestQuery["resultCode"] != "00")
            {
                var moMoInsert = new MoMoModel
                {
                    OrderId = requestQuery["orderId"],
                    FullName = User.FindFirstValue(ClaimTypes.Email),
                    Amount = double.Parse(requestQuery["amount"]),
                    OrderInfo = requestQuery["orderInfo"],
                    CreatedDate = DateTime.Now

                };
                _dataContext.Add(moMoInsert);
                await _dataContext.SaveChangesAsync();
                var PaymentMethod = "MoMo";
                await Checkout(PaymentMethod, requestQuery["orderId"]);
            }
            else
            {
                TempData["error"] = "Checkout With MoMo Failed!!!";
                return RedirectToAction("Index", "Home");
            }

            return View(response);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnPay()
        {
            var response = _vnPayService.PaymentExecute(HttpContext.Request.Query);

            if (response.VnPayResponseCode == "00")
            {
                var vnPayInsert = new VnPayModel
                {
                    OrderId = response.OrderId,
                    PaymentMethod = response.PaymentMethod,
                    OrderDescription = response.OrderDescription,
                    TransactionId = response.TransactionId,
                    PaymentId = response.PaymentId,
                    CreatedDate = DateTime.Now
                };
                _dataContext.Add(vnPayInsert);
                await _dataContext.SaveChangesAsync();
                var PaymentMethod = response.PaymentMethod;
                await Checkout(PaymentMethod, response.OrderId);
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
