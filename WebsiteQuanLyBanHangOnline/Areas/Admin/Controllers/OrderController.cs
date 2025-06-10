using Azure;
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
    public class OrderController : Controller
    {
        private readonly DataContext _dataContext;
        public OrderController(DataContext context)
        {
            _dataContext = context;
        }
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            List<OrderModel> orders = await _dataContext.Orders.OrderByDescending(c => c.CreatedDate).ToListAsync();

            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }

            int count = orders.Count;
            var pager = new Paginate(count, page, pageSize);
            int skip = (page - 1) * pageSize;

            var data = orders.Skip(skip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);

            //return View(await _dataContext.Orders.OrderByDescending(c => c.CreatedDate).ToListAsync());
        }
        [HttpGet]
        public async Task<IActionResult> View(string orderCode)
        {
            var orderDetail = await _dataContext.OrderDetails.Include(c => c.Product).Where(c => c.OrderCode == orderCode).ToListAsync();
            var Order = _dataContext.Orders.Where(o => o.OrderCode == orderCode).First();
            ViewBag.Status = Order.Status;
            return View(orderDetail);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateView(string orderCode, int status)
        {
            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;

            try
            {
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true, message = "Update Status Successfully!!!" });
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentMoMo(string OrderId)
        {
            var moMo = await _dataContext.MoMos.FirstOrDefaultAsync(x => x.OrderId == OrderId);
            if (moMo == null)
            {
                return NotFound();
            }
            return View(moMo);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentVnPay(string OrderId)
        {
            var vnPay = await _dataContext.VnPays.FirstOrDefaultAsync(x => x.OrderId == OrderId);
            if (vnPay == null)
            {
                return NotFound();
            }
            return View(vnPay);
        }

    }
}
