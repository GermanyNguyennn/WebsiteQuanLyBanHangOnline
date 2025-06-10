using WebsiteQuanLyBanHangOnline.Models.VnPay;

namespace WebsiteQuanLyBanHangOnline.Services.VnPay
{
    public interface IVnPayService
    {
        string CreatePayment(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
