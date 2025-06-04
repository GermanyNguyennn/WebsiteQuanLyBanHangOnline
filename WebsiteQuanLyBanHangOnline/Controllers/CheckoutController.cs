using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using WebsiteQuanLyBanHangOnline.Areas.Admin.Repository;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;
namespace WebsiteQuanLyBanHangOnline.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IEmailSender _emailSender;
        public CheckoutController(DataContext context, IEmailSender emailSender)
        {
            _dataContext = context;
            _emailSender = emailSender;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Checkout()
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
                orderItem.CreatedDate = DateTime.Now;

                var shippingPriceCookie = Request.Cookies["ShippingPrice"];
                decimal shippingPrice = 0;

                if (shippingPriceCookie != null)
                {
                    var shippingPriceJson = shippingPriceCookie;
                    shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
                }
                orderItem.ShippingCost = shippingPrice;
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
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
    }
}
