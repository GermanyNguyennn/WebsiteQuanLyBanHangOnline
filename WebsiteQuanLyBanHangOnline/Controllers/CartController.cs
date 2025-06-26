using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        private List<CartModel> GetCart()
        {
            return HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new();
        }

        private void SaveCart(List<CartModel> cart)
        {
            if (cart == null || !cart.Any())
            {
                HttpContext.Session.Remove("Cart");
                HttpContext.Session.Remove("AppliedCoupon");
                HttpContext.Session.Remove("DiscountAmount");
            }
            else
            {
                HttpContext.Session.SetJson("Cart", cart);
            }
        }

        private void RecalculateDiscount()
        {
            var cart = GetCart();
            if (!cart.Any()) return;

            var couponCode = HttpContext.Session.GetString("AppliedCoupon");
            if (string.IsNullOrEmpty(couponCode)) return;

            var coupon = _dataContext.Coupons.FirstOrDefault(c =>
                c.CouponCode == couponCode &&
                c.Status == 1 &&
                c.Quantity > 0 &&
                c.StartDate <= DateTime.Now &&
                c.EndDate >= DateTime.Now);

            if (coupon == null)
            {
                HttpContext.Session.Remove("AppliedCoupon");
                HttpContext.Session.Remove("DiscountAmount");
                return;
            }

            var total = cart.Sum(x => x.Quantity * x.Price);
            var discount = coupon.DiscountType == DiscountType.Percent
                ? (total * coupon.DiscountValue) / 100
                : coupon.DiscountValue;

            HttpContext.Session.SetString("DiscountAmount", discount.ToString());
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var cart = GetCart();

            var info = await _dataContext.Information.FirstOrDefaultAsync(x => x.UserId == userId);

            var viewModel = new CartViewModel
            {
                Cart = cart,
                TotalAmount = cart.Sum(x => x.Quantity * x.Price),
                FullName = user?.FullName ?? "",
                Email = user?.Email ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                Information = new InformationViewModel
                {
                    Address = info?.Address ?? "",
                    City = info != null ? await _locationService.GetCityNameById(info.City) : "",
                    District = info != null ? await _locationService.GetDistrictNameById(info.City, info.District) : "",
                    Ward = info != null ? await _locationService.GetWardNameById(info.District, info.Ward) : ""
                }
            };

            var couponCode = HttpContext.Session.GetString("AppliedCoupon");
            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = await _dataContext.Coupons.FirstOrDefaultAsync(c =>
                    c.CouponCode == couponCode &&
                    c.Status == 1 &&
                    c.Quantity > 0 &&
                    c.StartDate <= DateTime.Now &&
                    c.EndDate >= DateTime.Now);

                if (coupon != null)
                {
                    var discount = coupon.DiscountType == DiscountType.Percent
                        ? (viewModel.TotalAmount * coupon.DiscountValue) / 100
                        : coupon.DiscountValue;

                    viewModel.CouponCode = couponCode;
                    viewModel.DiscountAmount = discount;

                    HttpContext.Session.SetString("DiscountAmount", discount.ToString());
                }
                else
                {
                    HttpContext.Session.Remove("AppliedCoupon");
                    HttpContext.Session.Remove("DiscountAmount");
                }
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Add(int Id)
        {
            var product = await _dataContext.Products.FindAsync(Id);
            if (product == null) return NotFound();

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == Id);

            if (item == null)
                cart.Add(new CartModel(product));
            else
                item.Quantity++;

            SaveCart(cart);
            TempData["success"] = "Thêm Vào Giỏ Hàng Thành Công.";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        public IActionResult Increase(int Id)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == Id);
            if (product == null) return NotFound();

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == Id);

            if (item != null)
            {
                if (item.Quantity < product.Quantity)
                    item.Quantity++;
                else
                    TempData["error"] = "Đã Đạt Số Lượng Sản Phẩm Tối Đa.";
            }

            SaveCart(cart);
            RecalculateDiscount();

            return RedirectToAction("Index");
        }

        public IActionResult Decrease(int Id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == Id);

            if (item == null) return RedirectToAction("Index");

            if (item.Quantity > 1)
                item.Quantity--;
            else
                cart.Remove(item);

            SaveCart(cart);
            RecalculateDiscount();

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int Id)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProductId == Id);

            SaveCart(cart);
            RecalculateDiscount();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string CouponCode)
        {
            if (string.IsNullOrWhiteSpace(CouponCode))
            {
                TempData["error"] = "Vui Lòng Nhập Mã Giảm Giá!!!";
                return RedirectToAction("Index");
            }

            var coupon = await _dataContext.Coupons.FirstOrDefaultAsync(c =>
                c.CouponCode == CouponCode &&
                c.Status == 1 &&
                c.Quantity > 0 &&
                c.StartDate <= DateTime.Now &&
                c.EndDate >= DateTime.Now);

            if (coupon == null)
            {
                TempData["error"] = "Mã Giảm Giá Không Hợp Lệ Hoặc Đã Hết Hạn!!!";
                return RedirectToAction("Index");
            }

            var cart = GetCart();
            var total = cart.Sum(x => x.Quantity * x.Price);
            var discount = coupon.DiscountType == DiscountType.Percent
                ? (total * coupon.DiscountValue) / 100
                : coupon.DiscountValue;

            HttpContext.Session.SetString("AppliedCoupon", CouponCode);
            HttpContext.Session.SetString("DiscountAmount", discount.ToString());

            TempData["success"] = "Áp Dụng Mã Giảm Giá Thành Công!";
            return RedirectToAction("Index");
        }
    }
}
