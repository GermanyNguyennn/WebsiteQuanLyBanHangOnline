using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ShippingController : Controller
    {
        private readonly DataContext _dataContext;
        public ShippingController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            int count = await _dataContext.Brands.CountAsync();
            var pager = new Paginate(count, page, pageSize);

            var shippings = await _dataContext.Shippings
            .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            ViewBag.Shippings = shippings;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddShipping(ShippingModel shippingModel, string phuong, string quan, string tinh, decimal price)
        {
            if (string.IsNullOrWhiteSpace(tinh) || string.IsNullOrWhiteSpace(quan) || string.IsNullOrWhiteSpace(phuong))
            {
                return BadRequest(new { error = "Invalid Location Data." });
            }

            shippingModel.City = tinh;
            shippingModel.District = quan;
            shippingModel.Ward = phuong;
            shippingModel.Price = price;
            shippingModel.CreatedDate = DateTime.UtcNow;

            bool exists = await _dataContext.Shippings
                .AnyAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);

            if (exists)
            {
                return Ok(new { duplicate = true });
            }

            try
            {
                _dataContext.Shippings.Add(shippingModel);
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal Server Error.", detail = ex.Message });
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var shipping = await _dataContext.Shippings.FindAsync(id);
            if (shipping == null)
            {
                TempData["Error"] = "Shipping Not Found.";
                return RedirectToAction("Index");
            }

            _dataContext.Shippings.Remove(shipping);
            await _dataContext.SaveChangesAsync();

            TempData["Success"] = "Shipping Deleted Successfully!";
            return RedirectToAction("Index");
        }
    }
}
