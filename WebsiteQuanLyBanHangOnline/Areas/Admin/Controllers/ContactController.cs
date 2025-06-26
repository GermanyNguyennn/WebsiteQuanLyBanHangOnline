using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ContactController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ContactController(DataContext dataContext, IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = dataContext;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var contacts = await _dataContext.Contacts.ToListAsync();
            return View(contacts);
        }

        [HttpGet]
        public IActionResult Add()
        {
            // Không cho phép thêm nhiều Contact nếu đã tồn tại
            if (_dataContext.Contacts.Any())
            {
                TempData["error"] = "Đã tồn tại thông tin liên hệ. Vui lòng chỉnh sửa thay vì thêm mới.";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ContactModel contactModel)
        {
            if (!ModelState.IsValid)
                return HandleModelError(contactModel);

            if (contactModel.ImageUpload != null)
                contactModel.LogoImage = await SaveImageAsync(contactModel.ImageUpload);
            else
                contactModel.LogoImage = "null.jpg";

            _dataContext.Contacts.Add(contactModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Thêm thông tin liên hệ thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var contact = await _dataContext.Contacts.FirstOrDefaultAsync();
            if (contact == null)
            {
                TempData["error"] = "Không tìm thấy thông tin liên hệ.";
                return RedirectToAction("Index");
            }

            return View(contact);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ContactModel contactModel)
        {
            var existedContact = await _dataContext.Contacts.FirstOrDefaultAsync();
            if (existedContact == null)
            {
                TempData["error"] = "Không tìm thấy thông tin liên hệ.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
                return HandleModelError(contactModel);

            if (contactModel.ImageUpload != null)
                existedContact.LogoImage = await SaveImageAsync(contactModel.ImageUpload);

            existedContact.Name = contactModel.Name;
            existedContact.Description = contactModel.Description;
            existedContact.Map = contactModel.Map;
            existedContact.Email = contactModel.Email;
            existedContact.Phone = contactModel.Phone;
            existedContact.Address = contactModel.Address;

            _dataContext.Update(existedContact);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật thông tin liên hệ thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var contact = await _dataContext.Contacts.FindAsync(id);
            if (contact == null)
            {
                TempData["error"] = "Không tìm thấy thông tin liên hệ.";
                return RedirectToAction("Index");
            }

            // Xóa logo nếu không phải mặc định
            if (!string.Equals(contact.LogoImage, "null.jpg", StringComparison.OrdinalIgnoreCase))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "media/logo", contact.LogoImage);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _dataContext.Contacts.Remove(contact);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Xóa thông tin liên hệ thành công!";
            return RedirectToAction("Index");
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/logo");

            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            string imageName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
            string filePath = Path.Combine(uploadsDir, imageName);

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fs);
            }

            return imageName;
        }

        private IActionResult HandleModelError(ContactModel contactModel)
        {
            TempData["error"] = "Dữ liệu không hợp lệ.";

            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            string errorMessage = string.Join("\n", errors);
            return BadRequest(errorMessage);
        }
    }
}
