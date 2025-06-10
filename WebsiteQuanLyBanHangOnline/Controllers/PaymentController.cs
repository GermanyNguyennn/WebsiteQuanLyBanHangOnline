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
        public async Task<IActionResult> CreatePaymentUrlMoMo(MoMoInformationExecuteResponseModel model)
        {
            var response = await _moMoService.CreatePayment(model);
            return Redirect(response.PayUrl);
        }

        //[HttpGet]
        //public IActionResult PaymentCallBackMoMo()
        //{
        //    var response = _moMoService.PaymentExecute(HttpContext.Request.Query);
        //    return View(response);
        //}

        [HttpPost]
        public IActionResult CreatePaymentUrlVnPay(PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePayment(model, HttpContext);
            return Redirect(url);
        }

        //[HttpGet]
        //public IActionResult PaymentCallBackVnPay()
        //{
        //    var response = _vnPayService.PaymentExecute(Request.Query);
        //    return Json(response);
        //}

    }
}
