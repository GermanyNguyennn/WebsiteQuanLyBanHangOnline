using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponController : Controller
    {
        private readonly DataContext _dataContext;
        public CouponController(DataContext context)
        {
            _dataContext = context;
        }
        // Hiển thị danh sách coupon
        public async Task<IActionResult> Index()
        {
            var model = new CouponModel
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now
            };

            ViewBag.Coupons = _dataContext.Coupons.ToList();
            return View(model);
        }

        // GET: Form tạo mới
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        // POST: Xử lý tạo mới coupon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CouponModel couponModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid Data!!!";
                return View(couponModel);
            }

            if (couponModel.StartDate >= couponModel.EndDate)
            {
                TempData["error"] = "Start Date Must Be Less Than End Date!!!";
                return View(couponModel);
            }

            var isExist = await _dataContext.Coupons
                .AnyAsync(c => c.Name.ToLower() == couponModel.Name.ToLower());

            if (isExist)
            {
                TempData["error"] = "Coupon Already Exists!!!";
                return View(couponModel);
            }

            _dataContext.Coupons.Add(couponModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Added Coupon Successfully!!!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var coupon = await _dataContext.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();
            return View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CouponModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var coupon = await _dataContext.Coupons.FindAsync(model.Id);
            if (coupon == null) return NotFound();

            coupon.Name = model.Name;
            coupon.Description = model.Description;
            coupon.DiscountType = model.DiscountType;
            coupon.DiscountValue = model.DiscountValue;
            coupon.StartDate = model.StartDate;
            coupon.EndDate = model.EndDate;
            coupon.Quantity = model.Quantity;
            coupon.Status = model.Status;

            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Updated Coupon Successfully!!!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int Id)
        {
            var coupon = await _dataContext.Coupons.FindAsync(Id);

            if (coupon == null)
            {
                return NotFound();
            }

            _dataContext.Coupons.Remove(coupon);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Deleted Coupon Successfully!!!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int Id)
        {
            var coupon = await _dataContext.Coupons.FindAsync(Id);
            if (coupon == null) return NotFound();

            coupon.Status = coupon.Status == 1 ? 0 : 1;
            await _dataContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
