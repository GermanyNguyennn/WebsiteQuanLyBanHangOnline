using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly DataContext _dataContext;
        public ProductController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IActionResult> Index()
        {
            var products = _dataContext.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToArrayAsync();

            ViewBag.Sliders = await _dataContext.Sliders
               .Where(s => s.Status == 1)
               .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Detail(int Id)
        {
            var product = await _dataContext.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == Id);

            if (product == null)
            {
                return RedirectToAction("Index");
            }

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction("Index", "Home");
            }

            var products = await _dataContext.Products
                .Where(p => p.Name.Contains(searchTerm))
                .ToListAsync();

            ViewBag.Sliders = await _dataContext.Sliders
               .Where(s => s.Status == 1)
               .ToListAsync();

            ViewBag.Keyword = searchTerm;
            return View(products);
        }
    }
}
