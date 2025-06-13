using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;
using WebsiteQuanLyBanHangOnline.Services.Location;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly ILocationService _locationService;
        public CartController(DataContext context, UserManager<AppUserModel> userManager, ILocationService locationService)
        {
            _dataContext = context;
            _userManager = userManager;
            _locationService = locationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new();

            var info = await _dataContext.Information.FirstOrDefaultAsync(x => x.UserId == userId);

            string cityName = "", districtName = "", wardName = "";

            if (info != null)
            {
                cityName = await _locationService.GetCityNameById(info.City);
                districtName = await _locationService.GetDistrictNameById(info.City, info.District);
                wardName = await _locationService.GetWardNameById(info.District, info.Ward);
            }

            var viewModel = new CartViewModel
            {
                Cart = cart,
                GrandTotal = cart.Sum(x => x.Quantity * x.Price),
                Information = new InformationViewModel
                {
                    Address = info?.Address ?? "",
                    City = cityName,
                    District = districtName,
                    Ward = wardName
                }
            };

            return View(viewModel);
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
            TempData["success"] = "Add To Cart Success!!!";
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
                TempData["error"] = "Maximum Product Quantity Reached.";
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
    }
}
