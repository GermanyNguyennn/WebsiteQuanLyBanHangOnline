using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var shippings = await _dataContext.Shippings.OrderBy(c => c.Id).ToListAsync();

            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }

            int count = shippings.Count;
            var pager = new Paginate(count, page, pageSize);
            int skip = (page - 1) * pageSize;

            var pagedShippings = shippings.Skip(skip).Take(pager.PageSize).ToList();

            ViewBag.Shippings = pagedShippings; // giữ nguyên tên biến cũ
            ViewBag.Pager = pager;

            return View();
        }

        [HttpPost]

        public async Task<IActionResult> AddShipping(ShippingModel shippingModel, string phuong, string quan, string tinh, decimal price)
        {
            shippingModel.City = tinh;
            shippingModel.District = quan;
            shippingModel.Ward = phuong;
            shippingModel.Price = price;
            shippingModel.CreatedDate = DateTime.Now;

            try
            {
                var existingShipping = await _dataContext.Shippings.AnyAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);

                if (existingShipping)
                {
                    return Ok( new { duplicate = true });
                }
                _dataContext.Shippings.Add(shippingModel);
                await _dataContext.SaveChangesAsync();
                return Ok( new { success = true });
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> Delete(int Id)
        {
            ShippingModel shippingModel = await _dataContext.Shippings.FindAsync(Id);

            _dataContext.Shippings.Remove(shippingModel);
            await _dataContext.SaveChangesAsync();
            TempData["Success"] = "Delete Shipping Successfully!!!";
            return RedirectToAction("Index");
        }
    }
}
