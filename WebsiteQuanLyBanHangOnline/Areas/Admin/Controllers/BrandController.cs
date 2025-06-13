using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Authorize(Roles = "Admin")]
    public class BrandController : Controller
    {
        private readonly DataContext _dataContext;
        public BrandController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            int count = await _dataContext.Brands.CountAsync();
            var pager = new Paginate(count, page, pageSize);

            var data = await _dataContext.Brands
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            return View(data);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(BrandModel brandModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Model Validation Failed.";
                return View(brandModel);
            }

            brandModel.Slug = brandModel.Name.Trim().Replace(" ", "-");

            bool slugExists = await _dataContext.Brands
                .AnyAsync(b => b.Slug == brandModel.Slug);

            if (slugExists)
            {
                TempData["error"] = "Brand Already Exists In Database.";
                return View(brandModel);
            }

            _dataContext.Add(brandModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Brand Added Successfully!!!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var brandModel = await _dataContext.Brands.FindAsync(Id);
            if (brandModel == null)
            {
                TempData["error"] = "Brand Not Found.";
                return RedirectToAction("Index");
            }

            return View(brandModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BrandModel brandModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Model Validation Failed.";
                return View(brandModel);
            }

            brandModel.Slug = brandModel.Name.Trim().Replace(" ", "-");

            _dataContext.Update(brandModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Brand Updated Successfully!!!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int Id)
        {
            var brandModel = await _dataContext.Brands.FindAsync(Id);
            if (brandModel == null)
            {
                TempData["error"] = "Brand Not Found.";
                return RedirectToAction("Index");
            }

            _dataContext.Brands.Remove(brandModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Brand Deleted Successfully!!!";
            return RedirectToAction("Index");
        }
    }
}
