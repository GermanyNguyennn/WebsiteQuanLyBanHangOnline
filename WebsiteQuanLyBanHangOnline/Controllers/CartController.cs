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
            var user = await _userManager.FindByIdAsync(userId);
            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new();

            var info = await _dataContext.Information.FirstOrDefaultAsync(x => x.UserId == userId);

            string cityName = "", districtName = "", wardName = "";

            if (info != null)
            {
                cityName = await _locationService.GetCityNameById(info.City);
                districtName = await _locationService.GetDistrictNameById(info.City, info.District);
                wardName = await _locationService.GetWardNameById(info.District, info.Ward);
            }

            // Tính tổng tiền giỏ hàng
            decimal totalAmount = cart.Sum(x => x.Quantity * x.Price);

            if (cart.Count == 0)
            {
                HttpContext.Session.Remove("AppliedCoupon");
                HttpContext.Session.Remove("DiscountAmount");
            }

            // Khởi tạo ViewModel cơ bản
            var viewModel = new CartViewModel
            {
                Cart = cart,
                TotalAmount = totalAmount,
                FullName = user?.FullName ?? "",
                Email = user?.Email ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                Information = new InformationViewModel
                {
                    Address = info?.Address ?? "",
                    City = cityName,
                    District = districtName,
                    Ward = wardName
                }
            };

            // Kiểm tra mã giảm giá nếu có
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
                    decimal discountAmount = 0;
                    if (coupon.DiscountType == DiscountType.Percent)
                    {
                        discountAmount = (totalAmount * coupon.DiscountValue) / 100;
                    }
                    else
                    {
                        discountAmount = coupon.DiscountValue;
                    }

                    viewModel.CouponCode = couponCode;
                    viewModel.DiscountAmount = discountAmount;

                    // Cập nhật vào session
                    HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString());
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
            if (product == null)
                return NotFound();

            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new List<CartModel>();
            var cartModel = cart.FirstOrDefault(c => c.ProductId == Id);

            if (cartModel == null)
                cart.Add(new CartModel(product));
            else
                cartModel.Quantity += 1;

            HttpContext.Session.SetJson("Cart", cart);
            TempData["success"] = "Thêm Vào Giỏ Hàng Thành Công.";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        private void RecalculateDiscount()
        {
            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new();
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

            var grandTotal = cart.Sum(x => x.Quantity * x.Price);
            decimal discountAmount = coupon.DiscountType == DiscountType.Percent
                ? (grandTotal * coupon.DiscountValue) / 100
                : coupon.DiscountValue;

            HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString());
        }


        public IActionResult Increase(int Id)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == Id);
            if (product == null)
                return NotFound();

            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new();
            var cartModel = cart.FirstOrDefault(c => c.ProductId == Id);

            if (cartModel == null)
                return RedirectToAction("Index");

            if (cartModel.Quantity < product.Quantity)
            {
                cartModel.Quantity++;
            }
            else
            {
                TempData["error"] = "Đã Đạt Số Lượng Sản Phẩm Tối Đa.";
            }

            HttpContext.Session.SetJson("Cart", cart);

            RecalculateDiscount();

            return RedirectToAction("Index");
        }

        public IActionResult Decrease(int Id)
        {
            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new();

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
                RecalculateDiscount();
            }
            else
            {
                HttpContext.Session.Remove("Cart");
                HttpContext.Session.Remove("AppliedCoupon");
                HttpContext.Session.Remove("DiscountAmount");
            }

            return RedirectToAction("Index");
        }


        public IActionResult Delete(int Id)
        {
            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new();
            cart.RemoveAll(c => c.ProductId == Id);

            if (cart.Count == 0)
            {
                HttpContext.Session.Remove("Cart");
                HttpContext.Session.Remove("AppliedCoupon");
                HttpContext.Session.Remove("DiscountAmount");
            }
            else
            {
                HttpContext.Session.SetJson("Cart", cart);
                RecalculateDiscount();
            }

            return RedirectToAction("Index");
        }

        //public IActionResult Clear()
        //{
        //    HttpContext.Session.Remove("Cart");
        //    return RedirectToAction("Index");
        //}

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string CouponCode)
        {
            if (string.IsNullOrEmpty(CouponCode))
            {
                TempData["error"] = "Vui Lòng Nhập Mã Giảm Giá!!!";
                return RedirectToAction("Index");
            }

            var coupon = await _dataContext.Coupons
                .FirstOrDefaultAsync(c => c.CouponCode == CouponCode && c.Status == 1
                                          && c.Quantity > 0
                                          && c.StartDate <= DateTime.Now
                                          && c.EndDate >= DateTime.Now);

            if (coupon == null)
            {
                TempData["error"] = "Mã Giảm Giá Không Hợp Lệ Hoặc Đã Hết Hạn!!!";
                return RedirectToAction("Index");
            }

            var cart = HttpContext.Session.GetJson<List<CartModel>>("Cart") ?? new List<CartModel>();
            var grandTotal = cart.Sum(x => x.Quantity * x.Price);

            decimal discountAmount = 0;
            if (coupon.DiscountType == DiscountType.Percent)
            {
                discountAmount = (grandTotal * coupon.DiscountValue) / 100;
            }
            else
            {
                discountAmount = coupon.DiscountValue;
            }

            HttpContext.Session.SetString("AppliedCoupon", CouponCode);
            HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString()); // ✅ THÊM DÒNG NÀY

            TempData["success"] = "Áp Dụng Mã Giảm Giá Thành Công!";
            return RedirectToAction("Index");
        }
    }
}
