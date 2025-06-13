namespace WebsiteQuanLyBanHangOnline.Services.Location
{
    public interface ILocationService
    {
        Task<string> GetCityNameById(string cityId);
        Task<string> GetDistrictNameById(string cityId, string districtId);
        Task<string> GetWardNameById(string districtId, string wardId);
    }
}
