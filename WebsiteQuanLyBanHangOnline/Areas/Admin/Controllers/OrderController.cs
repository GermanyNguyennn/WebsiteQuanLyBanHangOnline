using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;
using WebsiteQuanLyBanHangOnline.Services.Location;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly ILocationService _locationService;

        public OrderController(DataContext context, UserManager<AppUserModel> userManager, ILocationService locationService)
        {
            _dataContext = context;
            _userManager = userManager;
            _locationService = locationService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            int count = await _dataContext.Orders.CountAsync();
            var pager = new Paginate(count, page, pageSize);

            var orders = await _dataContext.Orders
                .OrderBy(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> View(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return BadRequest("Mã đơn hàng không hợp lệ.");

            var order = await _dataContext.Orders
                .Include(o => o.Coupon)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            var orderDetails = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderCode == orderCode)
                .ToListAsync();

            ViewBag.Status = order.Status;
            ViewBag.OrderCode = orderCode;
            ViewBag.DiscountValue = order.Coupon?.DiscountValue ?? 0;
            ViewBag.DiscountType = order.Coupon?.DiscountType.ToString();

            return View(orderDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateView(string orderCode, int status)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return BadRequest(new { success = false, message = "Thiếu mã đơn hàng." });

            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });

            order.Status = status;

            try
            {
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật trạng thái thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi cập nhật trạng thái.", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentMoMoOrder(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest("Thiếu mã đơn hàng.");

            var moMo = await _dataContext.MoMos.FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (moMo == null)
                return NotFound("Không tìm thấy thông tin thanh toán MoMo.");

            return View(moMo);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentVNPayOrder(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest("Thiếu mã đơn hàng.");

            var vnPay = await _dataContext.VNPays.FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (vnPay == null)
                return NotFound("Không tìm thấy thông tin thanh toán VnPay.");

            return View(vnPay);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerInformation(string orderCode)
        {
            if (string.IsNullOrEmpty(orderCode))
                return NotFound("Thiếu mã đơn hàng.");

            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            var viewModel = new CartViewModel
            {
                FullName = order.FullName,
                Email = order.Email,
                PhoneNumber = order.PhoneNumber,
                Information = new InformationViewModel
                {
                    Address = order.Address,
                    City = order.City,
                    District = order.District,
                    Ward = order.Ward
                }
            };

            return View(viewModel);
        }
    }
}
