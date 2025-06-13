using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.MoMo;

namespace WebsiteQuanLyBanHangOnline.Services.MoMo
{
    public interface IMoMoService
    {
        Task<MoMoResponseModel>CreatePaymentAsync(MoMoInformationModel model);
        MoMoInformationModel PaymentExecuteAsync(IQueryCollection collection);
    }
}
