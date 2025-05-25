using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly DataContext _dataContext;
        public CategoryController(DataContext context)
        {
            _dataContext = context;
        }

        public async Task<IActionResult> Index(string Slug = "")
        {
            CategoryModel category = _dataContext.Categories.Where(c => c.Slug == Slug).FirstOrDefault();

            if (category == null)   return RedirectToAction("Index");

            var productsByCategory = _dataContext.Products.Where(c => c.CategoryId == category.Id);

            var sliders = _dataContext.Sliders.Where(c => c.Status == 1).ToList();

            ViewBag.Sliders = sliders;

            return View(await productsByCategory.OrderBy(c => c.Id).ToArrayAsync());
        }
    }
}
