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
                var shippingPriceJson = shippingPriceCookie;
                shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
            }
            CartViewModel cartViewModel = new()
            {
                Cart = cart,
                GrandTotal = cart.Sum(x => x.Quantity * x.Price),
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
            ProductModel product = await _dataContext.Products.FindAsync(Id);
            List<CartModel> cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new List<CartModel>();
            CartModel cartModel = cart.Where(c => c.ProductId == Id).FirstOrDefault();
            if (cartModel == null)
            {
                cart.Add(new CartModel(product));
            }
            else
            {
                cartModel.Quantity += 1;
            }
            HttpContext.Session.SetJson("Cart", cart);
            TempData["success"] = "Add To Cart Success!!!";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        public async Task<IActionResult> Decrease(int Id)
        {
            List<CartModel> cart = HttpContext.Session.GetJson<List<CartModel>>("Cart");
            CartModel cartModel = cart.Where(c => c.ProductId == Id).FirstOrDefault();
            if (cartModel.Quantity > 1)
            {
                --cartModel.Quantity;
            }
            else
            {
                cart.RemoveAll(c => c.ProductId == Id);
            }
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

        public async Task<IActionResult> Increase(int Id)
        {
            ProductModel productModel = await _dataContext.Products.Where(p => p.Id == Id).FirstOrDefaultAsync();
            List<CartModel> cart = HttpContext.Session.GetJson<List<CartModel>>("Cart");
            CartModel cartModel = cart.Where(c => c.ProductId == Id).FirstOrDefault();
            if (cartModel.Quantity >= 1 && productModel.Quantity > cartModel.Quantity)
            {
                ++cartModel.Quantity;
            }
            else
            {
                cartModel.Quantity = productModel.Quantity;
                TempData["Success"] = "Maximum Add Product Quantity To Cart!!! ";
            }
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

        public async Task<IActionResult> Clear(int Id)
        {
            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddShipping(ShippingModel shippingModel, string tinh, string quan, string phuong)
        {

            var existingShipping = await _dataContext.Shippings.FirstOrDefaultAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);

            decimal shippingPrice = 0;

            if (existingShipping != null)
            {
                shippingPrice = existingShipping.Price;
            }
            else
            {
                shippingPrice = 50000;
            }
            var shippingPriceJson = JsonConvert.SerializeObject(shippingPrice);
            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                    Secure = true
                };

                Response.Cookies.Append("ShippingPrice", shippingPriceJson, cookieOptions);
            }
            catch (Exception)
            {
               return NotFound();
            }
            return Json( new { shippingPrice });
        }
    }
}
