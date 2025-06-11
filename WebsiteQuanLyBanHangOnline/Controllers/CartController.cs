using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly DataContext _dataContext;
        public CartController(DataContext context)
        {
            _dataContext = context;
        }
        
        public IActionResult Index()
        {
            List<CartModel> cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new List<CartModel>();

            var shippingPriceCookie = Request.Cookies["ShippingPrice"];
            decimal shippingPrice = 0;

            if (shippingPriceCookie != null)
            {
                shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceCookie);
                Response.Cookies.Delete("ShippingPrice");
            }
            CartViewModel cartViewModel = new()
            {
                Cart = cart,
                GrandTotal = cart.Sum(x => x.Quantity * x.Price) + shippingPrice,
                ShippingPrice = shippingPrice
            };
            return View(cartViewModel);
        }

        public IActionResult Checkout()
        {
            return View("~/Views/Checkout/Index.cshtml");
        }
        public async Task<IActionResult> Add(int Id)
        {
            var product = await _dataContext.Products.FindAsync(Id);
            if (product == null)
                return NotFound();

            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new List<CartModel>();
            var cartModel = cart.FirstOrDefault(c => c.ProductId == Id);

            if (cartModel == null)
                cart.Add(new CartModel(product));
            else
                cartModel.Quantity += 1;

            HttpContext.Session.SetJson("Cart", cart);
            TempData["success"] = "Add To Cart Success!";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        public IActionResult Decrease(int Id)
        {
            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new List<CartModel>();

            var cartModel = cart.FirstOrDefault(c => c.ProductId == Id);
            if (cartModel == null)
                return RedirectToAction("Index");

            if (cartModel.Quantity > 1)
            {
                cartModel.Quantity--;
            }
            else
            {
                cart.RemoveAll(c => c.ProductId == Id);
            }

            if (cart.Any())
            {
                HttpContext.Session.SetJson("Cart", cart);
            }
            else
            {
                HttpContext.Session.Remove("Cart");
            }

            return RedirectToAction("Index");
        }

        public IActionResult Increase(int Id)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == Id);
            if (product == null)
                return NotFound();

            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new List<CartModel>();
            var cartModel = cart.FirstOrDefault(c => c.ProductId == Id);

            if (cartModel == null)
                return RedirectToAction("Index");

            if (cartModel.Quantity < product.Quantity)
            {
                cartModel.Quantity++;
            }
            else
            {
                TempData["success"] = "Maximum Product Quantity Reached.";
            }

            HttpContext.Session.SetJson("Cart", cart);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int Id)
        {
            List<CartModel> cart = HttpContext.Session.GetJson<List<CartModel>>("Cart");
            
            cart.RemoveAll(c => c.ProductId == Id);
            if (cart.Count == 0)
            {
                HttpContext.Session.Remove("Cart");
            }
            else
            {
                HttpContext.Session.SetJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> AddShipping(ShippingModel shippingModel, string tinh, string quan, string phuong)
        {
            if (string.IsNullOrEmpty(tinh) || string.IsNullOrEmpty(quan) || string.IsNullOrEmpty(phuong))
                return BadRequest("Địa chỉ không hợp lệ.");

            var shipping = await _dataContext.Shippings
                .FirstOrDefaultAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);

            decimal shippingPrice = shipping?.Price ?? 50000;

            var shippingPriceJson = JsonConvert.SerializeObject(shippingPrice);
            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                    Secure = false // nếu đang test local; khi lên production thì đặt true
                };

                Response.Cookies.Append("ShippingPrice", shippingPriceJson, cookieOptions);
            }
            catch
            {
                return StatusCode(500);
            }

            return Json(new { shippingPrice });
        }
    }
}
