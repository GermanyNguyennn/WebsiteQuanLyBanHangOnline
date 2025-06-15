using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.Statistical;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly DataContext _dataContext;
        public DashboardController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.CountProduct = _dataContext.Products.Count();
            ViewBag.CountOrder = _dataContext.Orders.Count();
            ViewBag.CountCategory = _dataContext.Categories.Count();
            ViewBag.CountUser = _dataContext.Users.Count();

            var query = _dataContext.OrderDetails
                .Include(od => od.Product)
                .Join(_dataContext.Orders,
                    od => od.OrderCode,
                    o => o.OrderCode,
                    (od, o) => new { od, o });

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.o.CreatedDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.o.CreatedDate.Date <= toDate.Value.Date);
            }

            var statistics = await query
                .GroupBy(g => new
                {
                    g.od.ProductId,
                    g.od.Product.Name,
                    g.od.Product.Image,
                    g.od.Product.ImportPrice
                })
                .Select(group => new StatisticalModel
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.Name,
                    Image = group.Key.Image,
                    TotalQuantitySold = group.Sum(x => x.od.Quantity),
                    TotalRevenue = group.Sum(x => x.od.Price * x.od.Quantity),
                    TotalCost = group.Sum(x => group.Key.ImportPrice * x.od.Quantity),
                    FirstSoldDate = group.Min(x => x.o.CreatedDate),
                    LastSoldDate = group.Max(x => x.o.CreatedDate)
                })
                .ToListAsync();

            var result = new StatisticalFilterModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Statistics = statistics
            };

            return View(result);
        }
    }
}
