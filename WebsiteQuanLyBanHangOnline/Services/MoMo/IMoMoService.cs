using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.MoMo;

namespace WebsiteQuanLyBanHangOnline.Services.MoMo
{
    public interface IMoMoService
    {
        Task<MoMoCreatePaymentResponseModel>CreatePaymentAsync(MoMoInformationExecuteResponseModel model);
        MoMoInformationExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}
