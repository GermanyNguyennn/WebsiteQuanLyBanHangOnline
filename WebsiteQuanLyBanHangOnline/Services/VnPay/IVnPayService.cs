using System.Threading.Tasks;
using WebsiteQuanLyBanHangOnline.Models.VnPay;

namespace WebsiteQuanLyBanHangOnline.Services.VnPay
{
    public interface IVnPayService
    {
        Task<string> CreatePaymentAsync(VNPayInformationModel model, HttpContext context);
        Task<VNPayResponseModel> PaymentExecuteAsync(IQueryCollection collections);
    }
}
