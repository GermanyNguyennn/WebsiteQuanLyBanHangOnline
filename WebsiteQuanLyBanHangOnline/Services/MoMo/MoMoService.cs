using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.MoMo;

namespace WebsiteQuanLyBanHangOnline.Services.MoMo
{
    public class MoMoService : IMoMoService
    {
        private readonly IOptions<MoMoOptionModel> _options;
        public MoMoService(IOptions<MoMoOptionModel> options)
        {
            _options = options;
        }
        public async Task<MoMoResponseModel> CreatePaymentAsync(MoMoInformationModel model)
        {
            var requestId = DateTime.UtcNow.Ticks.ToString();

            var rawData =
                $"accessKey={_options.Value.AccessKey}" +
                $"&amount={model.Amount}" +
                $"&extraData=" +
                $"&orderId={model.OrderId}" +
                $"&orderInfo={model.OrderInfo}" +
                $"&partnerCode={_options.Value.PartnerCode}" +
                $"&redirectUrl={_options.Value.ReturnUrl}" +
                $"&ipnUrl={_options.Value.NotifyUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={_options.Value.RequestType}";

            var signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

            var client = new RestClient(_options.Value.MoMoApiUrl);
            var request = new RestRequest() { Method = Method.Post };
            request.AddHeader("Content-Type", "application/json; charset=UTF-8");

            var requestData = new
            {
                accessKey = _options.Value.AccessKey,
                partnerCode = _options.Value.PartnerCode,
                requestType = _options.Value.RequestType,
                redirectUrl = _options.Value.ReturnUrl,
                ipnUrl = _options.Value.NotifyUrl,
                orderId = model.OrderId,
                amount = model.Amount.ToString(),
                orderInfo = model.OrderInfo,
                requestId = requestId,
                extraData = "",
                signature = signature
            };


            request.AddParameter("application/json", JsonConvert.SerializeObject(requestData), ParameterType.RequestBody);

            var response = await client.ExecuteAsync(request);
            var momoResponse = JsonConvert.DeserializeObject<MoMoResponseModel>(response.Content);
            return momoResponse;

        }

        public MoMoInformationModel PaymentExecuteAsync(IQueryCollection collection)
        {
            double amount = double.Parse(collection.First(s => s.Key == "amount").Value);
            var orderInfo = collection.First(s => s.Key == "orderInfo").Value;
            var orderId = collection.First(s => s.Key == "orderId").Value;

            return new MoMoInformationModel()
            {             
                OrderId = orderId,
                OrderInfo = orderInfo,
                Amount = amount,
                CreatedDate = DateTime.Now,
            };
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            byte[] hashBytes;

            using (var hmac = new HMACSHA256(keyBytes))
            {
                hashBytes = hmac.ComputeHash(messageBytes);
            }

            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return hashString;
        }
    }
}
