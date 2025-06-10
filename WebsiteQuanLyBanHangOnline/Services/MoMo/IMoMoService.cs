using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.MoMo;

namespace WebsiteQuanLyBanHangOnline.Services.MoMo
{
    public interface IMoMoService
    {
        Task<MoMoCreatePaymentResponseModel>CreatePayment(MoMoInformationExecuteResponseModel model);
        MoMoInformationExecuteResponseModel PaymentExecute(IQueryCollection collection);
    }
}
