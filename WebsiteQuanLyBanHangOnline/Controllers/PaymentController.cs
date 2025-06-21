using Microsoft.AspNetCore.Mvc;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.MoMo;
using WebsiteQuanLyBanHangOnline.Models.VnPay;
using WebsiteQuanLyBanHangOnline.Repository;
using WebsiteQuanLyBanHangOnline.Services.MoMo;
using WebsiteQuanLyBanHangOnline.Services.VnPay;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly IMoMoService _moMoService;
        private readonly DataContext _dataContext;
        public PaymentController(IMoMoService momoService, IVnPayService vnPayService, DataContext dataContext)
        {
            _moMoService = momoService;
            _vnPayService = vnPayService;
            _dataContext = dataContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentUrlMoMo(MoMoInformationModel model)
        {
            var response = await _moMoService.CreatePaymentAsync(model);

            if (response == null)
            {
                TempData["error"] = "MoMo Không Phản Hồi.";
                return RedirectToAction("Cart", "Index");
            }

            if (response.ErrorCode != 0)
            {
                TempData["error"] = $"Lỗi MoMo: {response.LocalMessage ?? response.Message} (Mã lỗi: {response.ErrorCode})";
                return RedirectToAction("Cart", "Index");
            }

            if (string.IsNullOrEmpty(response.PayUrl))
            {
                TempData["error"] = "Không Nhận Được Đường Dẫn Thanh Toán Từ MoMo.";
                return RedirectToAction("Cart", "Index");
            }

            return Redirect(response.PayUrl);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentUrlVnPay(VNPayInformationModel model)
        {
            var response = await _vnPayService.CreatePaymentAsync(model, HttpContext);

            if (string.IsNullOrEmpty(response))
            {
                TempData["error"] = "Không Nhận Được Đường Dẫn Thanh Toán Từ VNPay.";
                return RedirectToAction("Cart", "Index");
            }

            return Redirect(response);
        }
    }
}
