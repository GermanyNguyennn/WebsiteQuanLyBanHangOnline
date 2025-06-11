using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, DataContext context)
        {
            _logger = logger;
            _dataContext = context;
        }


        public async Task<IActionResult> Index(string sort_by = "", string startprice = "", string endprice = "")
        {
            var query = _dataContext.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

            // Lọc theo khoảng giá nếu có
            if (!string.IsNullOrEmpty(startprice) && !string.IsNullOrEmpty(endprice))
            {
                if (decimal.TryParse(startprice, out decimal startPriceVal) &&
                    decimal.TryParse(endprice, out decimal endPriceVal))
                {
                    query = query.Where(p => p.Price >= startPriceVal && p.Price <= endPriceVal);
                }
            }

            // Sắp xếp theo lựa chọn
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

            var products = await query.ToListAsync();

            var sliders = await _dataContext.Sliders
                .Where(s => s.Status == 1)
                .ToListAsync();

            ViewBag.Sliders = sliders;
            ViewBag.sort_key = sort_by;
            ViewBag.count = products.Count;

            if (products.Any())
            {
                ViewBag.minprice = products.Min(p => p.Price);
                ViewBag.maxprice = products.Max(p => p.Price);
            }
            else
            {
                ViewBag.minprice = 0;
                ViewBag.maxprice = 0;
            }

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int statuscode)
        {
            if (statuscode == 404)
            {
                return View("NotFound");
            }
            else
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }               
        }

        public async Task<IActionResult> Contact()
        {    
            var contact = await _dataContext.Contacts.FirstOrDefaultAsync();
            return View(contact);
        }
    }
}
