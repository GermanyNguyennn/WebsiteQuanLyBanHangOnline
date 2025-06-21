using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(ContactModel contactModel)
        {
            if (!ModelState.IsValid)
                return HandleModelError(contactModel);

            if (contactModel.ImageUpload != null)
                contactModel.LogoImage = await SaveImageAsync(contactModel.ImageUpload);

            _dataContext.Add(contactModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Contact Added Successfully!!!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var contact = await _dataContext.Contacts.FirstOrDefaultAsync();
            if (contact == null)
            {
                TempData["error"] = "Contact Not Found.";
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
                TempData["error"] = "Contact Not Found.";
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

            TempData["success"] = "Contact Updated Successfully!!!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int Id)
        {
            var contactModel = await _dataContext.Contacts.FindAsync(Id);
            if (contactModel == null)
            {
                TempData["error"] = "Contact Not Found.";
                return RedirectToAction("Index");
            }

            if (!string.Equals(contactModel.LogoImage, "null.jpg"))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "media/logo", contactModel.LogoImage);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _dataContext.Contacts.Remove(contactModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Contact Deleted Successfully!!!";
            return RedirectToAction("Index");
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/logo");
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
            TempData["error"] = "Models Have Some Problems!!!";

            var errors = ModelState.Values
                                   .SelectMany(v => v.Errors)
                                   .Select(e => e.ErrorMessage);

            string errorMessage = string.Join("\n", errors);
            return BadRequest(errorMessage);
        }

    }
}
