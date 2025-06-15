namespace WebsiteQuanLyBanHangOnline.Models.Statistical
{
    public class StatisticalFilterModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<StatisticalModel> Statistics { get; set; }
    }
}
