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

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int? categoryId, int? brandId, string statisticType = "day")
        {
            await LoadOverviewCounters();
            await LoadFilterSelections(categoryId, brandId, statisticType);

            var statistics = await GetStatisticalData(fromDate, toDate, categoryId, brandId);
            var orders = await GetFilteredOrders(fromDate, toDate);

            var revenueChartData = GetDailyRevenueChartData(orders);
            var monthlyChartData = GetMonthlyRevenueChartData(orders);
            var yearlyChartData = GetYearlyRevenueChartData(orders);

            BindChartViewBags(revenueChartData, monthlyChartData, yearlyChartData);

            return View(new StatisticalFilterModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Statistics = statistics
            });
        }

        private async Task LoadOverviewCounters()
        {
            ViewBag.CountProduct = await _dataContext.Products.CountAsync();
            ViewBag.CountOrder = await _dataContext.Orders.CountAsync();
            ViewBag.CountCategory = await _dataContext.Categories.CountAsync();
            ViewBag.CountUser = await _dataContext.Users.CountAsync();
        }

        private async Task LoadFilterSelections(int? categoryId, int? brandId, string statisticType)
        {
            ViewBag.Categories = await _dataContext.Categories.ToListAsync();
            ViewBag.Brands = await _dataContext.Brands.ToListAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedBrand = brandId;
            ViewBag.SelectedStatisticType = statisticType;
        }

        private async Task<List<StatisticalModel>> GetStatisticalData(DateTime? fromDate, DateTime? toDate, int? categoryId, int? brandId)
        {
            var query = _dataContext.OrderDetails
                .Include(od => od.Order)
                    .ThenInclude(o => o.Coupon)
                .Include(od => od.Product)
                .AsQueryable();

            // Lọc theo ngày và bộ lọc
            if (fromDate.HasValue)
                query = query.Where(x => x.Order.CreatedDate.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(x => x.Order.CreatedDate.Date <= toDate.Value.Date);

            if (categoryId.HasValue)
                query = query.Where(x => x.Product.CategoryId == categoryId);

            if (brandId.HasValue)
                query = query.Where(x => x.Product.BrandId == brandId);

            // Lấy dữ liệu về để xử lý
            var list = await query.ToListAsync();

            var statistics = list
                .GroupBy(x => new
                {
                    x.ProductId,
                    x.Product.Name,
                    x.Product.Image,
                    x.Product.ImportPrice
                })
                .Select(group =>
                {
                    var totalQuantity = group.Sum(x => x.Quantity);
                    var totalRevenue = group.Sum(x => x.Price * x.Quantity);
                    var totalCost = group.Sum(x => x.Quantity * x.Product.ImportPrice);

                    var withCoupon = group.Where(x => x.Order.CouponId != null).ToList();
                    var withoutCoupon = group.Where(x => x.Order.CouponId == null).ToList();

                    var revenueWithCoupon = withCoupon.Sum(x => x.Price * x.Quantity);
                    var costWithCoupon = withCoupon.Sum(x => x.Quantity * x.Product.ImportPrice);

                    var revenueWithoutCoupon = withoutCoupon.Sum(x => x.Price * x.Quantity);
                    var costWithoutCoupon = withoutCoupon.Sum(x => x.Quantity * x.Product.ImportPrice);

                    var totalDiscountCoupon = withCoupon.Sum(x =>
                    {
                        var total = x.Price * x.Quantity;
                        var discount = x.Order.Coupon == null ? 0 :
                            (x.Order.Coupon.DiscountType == DiscountType.Percent
                                ? total * x.Order.Coupon.DiscountValue / 100
                                : x.Order.Coupon.DiscountValue);
                        return Math.Min(discount, total);
                    });

                    return new StatisticalModel
                    {
                        ProductId = group.Key.ProductId,
                        ProductName = group.Key.Name,
                        Image = group.Key.Image,

                        TotalQuantitySold = totalQuantity,
                        TotalRevenue = totalRevenue,
                        TotalCost = totalCost,

                        QuantityWithCoupon = withCoupon.Sum(x => x.Quantity),
                        QuantityWithoutCoupon = withoutCoupon.Sum(x => x.Quantity),

                        RevenueWithCoupon = revenueWithCoupon,
                        RevenueWithoutCoupon = revenueWithoutCoupon,

                        CostWithCoupon = costWithCoupon,
                        CostWithoutCoupon = costWithoutCoupon,

                        TotalDiscountCoupon = totalDiscountCoupon,

                        FirstSoldDate = group.Min(x => x.Order.CreatedDate),
                        LastSoldDate = group.Max(x => x.Order.CreatedDate)
                    };
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            return statistics;
        }


        private async Task<List<OrderModel>> GetFilteredOrders(DateTime? fromDate, DateTime? toDate)
        {
            return await _dataContext.Orders
                .Where(o =>
                    (!fromDate.HasValue || o.CreatedDate.ToLocalTime().Date >= fromDate.Value.Date) &&
                    (!toDate.HasValue || o.CreatedDate.ToLocalTime().Date <= toDate.Value.Date))
                .Include(o => o.OrderDetails)
                .Include(o => o.Coupon)
                .ToListAsync();
        }

        private List<RevenueChartModel> GetDailyRevenueChartData(List<OrderModel> orders)
        {
            var data = orders
                .GroupBy(o => o.CreatedDate.ToLocalTime().Date)
                .Select(g => new RevenueChartModel
                {
                    Date = g.Key,
                    RevenueBeforeDiscount = g.Sum(order => order.OrderDetails.Sum(od => od.Price * od.Quantity)),
                    RevenueAfterDiscount = g.Sum(order =>
                    {
                        var total = order.OrderDetails.Sum(od => od.Price * od.Quantity);
                        var discount = order.Coupon == null ? 0 :
                            (order.Coupon.DiscountType == DiscountType.Percent ? total * order.Coupon.DiscountValue / 100 : order.Coupon.DiscountValue);
                        return Math.Max(total - discount, 0);
                    })
                })
                .OrderBy(x => x.Date)
                .ToList();

            return data;
        }

        private List<dynamic> GetMonthlyRevenueChartData(List<OrderModel> orders)
        {
            return orders
                .GroupBy(o => new { o.CreatedDate.Year, o.CreatedDate.Month })
                .Select(g => new
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1),
                    RevenueBefore = g.Sum(order => order.OrderDetails.Sum(od => od.Price * od.Quantity)),
                    RevenueAfter = g.Sum(order =>
                    {
                        var total = order.OrderDetails.Sum(od => od.Price * od.Quantity);
                        var discount = order.Coupon == null ? 0 :
                            (order.Coupon.DiscountType == DiscountType.Percent ? total * order.Coupon.DiscountValue / 100 : order.Coupon.DiscountValue);
                        return Math.Max(total - discount, 0);
                    })
                })
                .OrderBy(x => x.Month)
                .ToList<dynamic>();
        }

        private List<dynamic> GetYearlyRevenueChartData(List<OrderModel> orders)
        {
            return orders
                .GroupBy(o => o.CreatedDate.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    RevenueBefore = g.Sum(order => order.OrderDetails.Sum(od => od.Price * od.Quantity)),
                    RevenueAfter = g.Sum(order =>
                    {
                        var total = order.OrderDetails.Sum(od => od.Price * od.Quantity);
                        var discount = order.Coupon == null ? 0 :
                            (order.Coupon.DiscountType == DiscountType.Percent ? total * order.Coupon.DiscountValue / 100 : order.Coupon.DiscountValue);
                        return Math.Max(total - discount, 0);
                    })
                })
                .OrderBy(x => x.Year)
                .ToList<dynamic>();
        }

        private void BindChartViewBags(List<RevenueChartModel> daily, List<dynamic> monthly, List<dynamic> yearly)
        {
            ViewBag.RevenueChartData = daily;
            ViewBag.TotalRevenueBefore = daily.Sum(x => x.RevenueBeforeDiscount);
            ViewBag.TotalRevenueAfter = daily.Sum(x => x.RevenueAfterDiscount);
            ViewBag.TotalDiscount = ViewBag.TotalRevenueBefore - ViewBag.TotalRevenueAfter;

            ViewBag.MonthlyChartLabels = monthly.Select(x => ((DateTime)x.Month).ToString("MM/yyyy")).ToList();
            ViewBag.MonthlyRevenueBefore = monthly.Select(x => (decimal)x.RevenueBefore).ToList();
            ViewBag.MonthlyRevenueAfter = monthly.Select(x => (decimal)x.RevenueAfter).ToList();

            ViewBag.YearlyChartLabels = yearly.Select(x => x.Year.ToString()).ToList();
            ViewBag.YearlyRevenueBefore = yearly.Select(x => (decimal)x.RevenueBefore).ToList();
            ViewBag.YearlyRevenueAfter = yearly.Select(x => (decimal)x.RevenueAfter).ToList();
            ViewBag.YearlyTotalRevenue = yearly.ToDictionary(x => x.Year.ToString(), x => (decimal)x.RevenueAfter);
        }      
    }
}
