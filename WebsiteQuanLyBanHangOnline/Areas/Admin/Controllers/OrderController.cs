using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            int count = await _dataContext.Orders.CountAsync();
            var pager = new Paginate(count, page, pageSize);

            var data = await _dataContext.Orders
            .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            return View(data);
        }


        [HttpGet]
        public async Task<IActionResult> View(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return BadRequest("Invalid Order Code.");

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null)
                return NotFound("Order Not Found.");

            var orderDetails = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderCode == orderCode)
                .ToListAsync();

            ViewBag.Status = order.Status;
            ViewBag.OrderCode = orderCode;
            return View(orderDetails);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateView(string orderCode, int status)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return BadRequest(new { success = false, message = "Order Code is Required." });

            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null)
                return NotFound(new { success = false, message = "Order Not Found." });

            order.Status = status;

            try
            {
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true, message = "Status Updated Successfully!!!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed To Update Status.", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentMoMo(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest("Order ID Is Required.");

            var moMo = await _dataContext.MoMos.FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (moMo == null)
                return NotFound("MoMo Payment Info Not Found.");

            return View(moMo);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentVnPay(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest("Order ID Is Required.");

            var vnPay = await _dataContext.VnPays.FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (vnPay == null)
                return NotFound("VnPay Payment Info Not Found.");

            return View(vnPay);
        }
    }
}
