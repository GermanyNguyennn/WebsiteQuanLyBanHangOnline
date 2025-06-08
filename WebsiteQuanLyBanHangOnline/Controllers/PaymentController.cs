using Microsoft.AspNetCore.Mvc;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.VnPay;
using WebsiteQuanLyBanHangOnline.Services.MoMo;
using WebsiteQuanLyBanHangOnline.Services.VnPay;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private IMoMoService _moMoService;
        public PaymentController(IMoMoService momoService, IVnPayService vnPayService)
        {
            _moMoService = momoService;
            _vnPayService = vnPayService;
        }
        [HttpPost]
        public async Task<IActionResult> CreatePaymentUrlMoMo(OrderInfoModel model)
        {
            var response = await _moMoService.CreatePaymentAsync(model);
            return Redirect(response.PayUrl);
        }

        [HttpGet]
        public IActionResult PaymentCallBackMoMo()
        {
            var response = _moMoService.PaymentExecuteAsync(HttpContext.Request.Query);
            return View(response);
        }

        [HttpPost]
        public IActionResult CreatePaymentUrlVnPay(PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
            return Redirect(url);
        }

        [HttpGet]
        public IActionResult PaymentCallBackVnPay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            return Json(response);
        }

    }
}
