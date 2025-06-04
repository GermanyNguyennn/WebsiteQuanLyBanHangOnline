using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly DataContext _dataContext;
        public OrderController(DataContext context)
        {
            _dataContext = context;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Orders.OrderByDescending(c => c.CreatedDate).ToListAsync());
        }
        [HttpGet]
        public async Task<IActionResult> View(string orderCode)
        {
            var orderDetail = await _dataContext.OrderDetails.Include(c => c.Product).Where(c => c.OrderCode == orderCode).ToListAsync();
            return View(orderDetail);
        }
    }
}
