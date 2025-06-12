using WebsiteQuanLyBanHangOnline.Models.VnPay;

namespace WebsiteQuanLyBanHangOnline.Services.VnPay
{
    public interface IVnPayService
    {
        Task<string> CreatePaymentAsync(PaymentInformationModel model, HttpContext context);
        Task<PaymentResponseModel> PaymentExecuteAsync(IQueryCollection collections);
    }
}
