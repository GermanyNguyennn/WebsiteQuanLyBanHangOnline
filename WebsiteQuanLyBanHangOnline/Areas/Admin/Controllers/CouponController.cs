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
        public async Task<IActionResult> Index()
        {
            var coupons = await _dataContext.Coupons.OrderBy(c => c.Id).ToListAsync();
            ViewBag.Coupons = coupons;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CouponModel couponModel)
        {
            if (ModelState.IsValid)
            {
                _dataContext.Add(couponModel);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Coupon Added Successfully!!!";
                return RedirectToAction("Index");
            }

            TempData["error"] = "Models Have Some Problems!!!";

            string errorMessage = string.Join("\n",
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

            return BadRequest(errorMessage);
        }
    }
}
