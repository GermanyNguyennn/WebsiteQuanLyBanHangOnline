using System.Text.Json;

namespace WebsiteQuanLyBanHangOnline.Services.Location
{
    public class LocationService : ILocationService
    { 
        private readonly HttpClient _httpClient;

        public LocationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetCityNameById(string cityId)
        {
            var response = await _httpClient.GetStringAsync("https://esgoo.net/api-tinhthanh/1/0.htm");
            var result = JsonSerializer.Deserialize<LocationApiResponse>(response);
            return result?.data?.FirstOrDefault(c => c.id == cityId)?.full_name ?? "";
        }

        public async Task<string> GetDistrictNameById(string cityId, string districtId)
        {
            var response = await _httpClient.GetStringAsync($"https://esgoo.net/api-tinhthanh/2/{cityId}.htm");
            var result = JsonSerializer.Deserialize<LocationApiResponse>(response);
            return result?.data?.FirstOrDefault(d => d.id == districtId)?.full_name ?? "";
        }

        public async Task<string> GetWardNameById(string districtId, string wardId)
        {
            var response = await _httpClient.GetStringAsync($"https://esgoo.net/api-tinhthanh/3/{districtId}.htm");
            var result = JsonSerializer.Deserialize<LocationApiResponse>(response);
            return result?.data?.FirstOrDefault(w => w.id == wardId)?.full_name ?? "";
        }

        private class LocationApiResponse
        {
            public int error { get; set; }
            public List<LocationItem> data { get; set; }
        }
        private class LocationItem
        {
            public string id { get; set; }
            public string full_name { get; set; }
        }
    }
}
