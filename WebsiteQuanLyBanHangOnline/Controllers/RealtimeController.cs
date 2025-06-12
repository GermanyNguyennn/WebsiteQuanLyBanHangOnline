using Microsoft.AspNetCore.Mvc;

namespace WebsiteQuanLyBanHangOnline.Controllers
{
    public class RealtimeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}
