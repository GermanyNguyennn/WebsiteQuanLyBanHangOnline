using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly DataContext _dataContext;
        public CategoryController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            int count = await _dataContext.Categories.CountAsync();
            var pager = new Paginate(count, page, pageSize);

            var data = await _dataContext.Categories
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
        public async Task<IActionResult> Add(CategoryModel categoryModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Model Validation Failed.";
                return View(categoryModel);
            }

            categoryModel.Slug = categoryModel.Name.Trim().Replace(" ", "-");

            bool slugExists = await _dataContext.Categories
                .AnyAsync(c => c.Slug == categoryModel.Slug);

            if (slugExists)
            {
                TempData["error"] = "Category Already Exists.";
                return View(categoryModel);
            }

            _dataContext.Add(categoryModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Category Added Successfully!!!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var categoryModel = await _dataContext.Categories.FindAsync(Id);
            if (categoryModel == null)
            {
                TempData["error"] = "Category Not Found.";
                return RedirectToAction("Index");
            }

            return View(categoryModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryModel categoryModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Model Validation Failed.";
                return View(categoryModel);
            }

            categoryModel.Slug = categoryModel.Name.Trim().Replace(" ", "-");

            _dataContext.Update(categoryModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Category Updated Successfully!!!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int Id)
        {
            var categoryModel = await _dataContext.Categories.FindAsync(Id);
            if (categoryModel == null)
            {
                TempData["error"] = "Category Not Found.";
                return RedirectToAction("Index");
            }

            _dataContext.Categories.Remove(categoryModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Category Deleted Successfully!!!";
            return RedirectToAction("Index");
        }
    }
}
