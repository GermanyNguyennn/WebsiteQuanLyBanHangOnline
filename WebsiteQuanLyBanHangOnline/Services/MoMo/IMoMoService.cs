using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.MoMo;

namespace WebsiteQuanLyBanHangOnline.Services.MoMo
{
    public interface IMoMoService
    {
        Task<MoMoCreatePaymentResponseModel>CreatePaymentAsync(OrderInfoModel model);
        MoMoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}
