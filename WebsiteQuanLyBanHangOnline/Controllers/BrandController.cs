using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    [Authorize]
    public class BrandController : Controller
    {
        private readonly DataContext _dataContext;
        public BrandController(DataContext context)
        {
            _dataContext = context;
        }

        //public async Task<IActionResult> Index(string Slug = "")
        //{
        //    var brand = await _dataContext.Brands
        //        .Where(b => b.Slug == Slug)
        //        .FirstOrDefaultAsync();

        //    if (brand == null)
        //        return RedirectToAction("Index", "Home");

        //    var productsByBrand = await _dataContext.Products
        //        .Where(p => p.BrandId == brand.Id)
        //        .OrderBy(p => p.Id)
        //        .ToListAsync();

        //    var sliders = await _dataContext.Sliders
        //        .Where(s => s.Status == 1)
        //        .ToListAsync();

        //    ViewBag.Sliders = sliders;

        //    return View(productsByBrand);
        //}

        public async Task<IActionResult> Index(string Slug = "", string sort_by = "", string startprice = "", string endprice = "")
        {
            var brand = await _dataContext.Brands
                .Where(c => c.Slug == Slug)
                .FirstOrDefaultAsync();

            if (brand == null)
                return RedirectToAction("Index", "Home");

            var query = _dataContext.Products
                .Where(p => p.BrandId == brand.Id);

            // Lọc theo giá nếu có
            if (!string.IsNullOrEmpty(startprice) && !string.IsNullOrEmpty(endprice))
            {
                if (decimal.TryParse(startprice, out decimal startPriceVal) &&
                    decimal.TryParse(endprice, out decimal endPriceVal))
                {
                    query = query.Where(p => p.Price >= startPriceVal && p.Price <= endPriceVal);
                }
            }

            // Sắp xếp
            switch (sort_by)
            {
                case "price_increase":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_decrease":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "price_newest":
                    query = query.OrderByDescending(p => p.Id);
                    break;
                case "price_oldest":
                    query = query.OrderBy(p => p.Id);
                    break;
                default:
                    query = query.OrderByDescending(p => p.Id);
                    break;
            }

            var productsByCategory = await query.ToListAsync();

            ViewBag.Sliders = await _dataContext.Sliders
                .Where(s => s.Status == 1)
                .ToListAsync();

            ViewBag.count = productsByCategory.Count;

            if (productsByCategory.Any())
            {
                ViewBag.minprice = productsByCategory.Min(p => p.Price);
                ViewBag.maxprice = productsByCategory.Max(p => p.Price);
            }
            else
            {
                ViewBag.minprice = 0;
                ViewBag.maxprice = 0;
            }

            ViewBag.sort_key = sort_by;

            return View(productsByCategory);
        }
    }
}
