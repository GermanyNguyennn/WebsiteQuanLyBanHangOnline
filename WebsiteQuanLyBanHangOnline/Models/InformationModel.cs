using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteQuanLyBanHangOnline.Models
{
    public class InformationModel
    {
        [Key]
        public string Id { get; set; }

        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUserModel User { get; set; }

        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
    }
}
