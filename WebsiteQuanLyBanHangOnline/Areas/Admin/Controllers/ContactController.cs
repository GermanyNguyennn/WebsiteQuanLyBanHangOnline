using Microsoft.AspNetCore.Authorization;
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
        private readonly IWebHostEnvironment _webHostEnviroment;
        public ContactController(DataContext dataContext, IWebHostEnvironment webHostEnviroment)
        {
            _dataContext = dataContext;
            _webHostEnviroment = webHostEnviroment;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Contacts.ToListAsync());
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        public async Task<IActionResult> Add(ContactModel contactModel)
        {

            if (ModelState.IsValid)
            {

                if (contactModel.ImageUpload != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnviroment.WebRootPath, "media/logo");
                    string imageName = Guid.NewGuid().ToString() + "_" + contactModel.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsDir, imageName);

                    FileStream fs = new FileStream(filePath, FileMode.Create);
                    await contactModel.ImageUpload.CopyToAsync(fs);
                    fs.Close();
                    contactModel.LogoImage = imageName;
                }

                _dataContext.Add(contactModel);
                await _dataContext.SaveChangesAsync();
                TempData["Success"] = "Add Contact Successfully!!!";
                return RedirectToAction("Index");

            }
            else
            {
                TempData["Error"] = "Models Have Some Problems!!!";
                List<string> errors = new List<string>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }
            return View(contactModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            ContactModel contactModel = await _dataContext.Contacts.FirstOrDefaultAsync();
            return View(contactModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ContactModel contactModel)
        {
            var existed_contact = _dataContext.Contacts.FirstOrDefault();

            if (ModelState.IsValid)
            {

                if (contactModel.ImageUpload != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnviroment.WebRootPath, "media/logo");
                    string imageName = Guid.NewGuid().ToString() + "_" + contactModel.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsDir, imageName);

                    FileStream fs = new FileStream(filePath, FileMode.Create);
                    await contactModel.ImageUpload.CopyToAsync(fs);
                    fs.Close();
                    existed_contact.LogoImage = imageName;
                }

                existed_contact.Name = contactModel.Name;             
                existed_contact.Description = contactModel.Description;
                existed_contact.Map = contactModel.Map;
                existed_contact.Email = contactModel.Email;
                existed_contact.Phone = contactModel.Phone;              

                _dataContext.Update(existed_contact);
                await _dataContext.SaveChangesAsync();
                TempData["Success"] = "Update Contact Successfully!!!";
                return RedirectToAction("Index");

            }
            else
            {
                TempData["Error"] = "Models Have Some Problems!!!";
                List<string> errors = new List<string>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }
            return View(contactModel);
        }

        public async Task<IActionResult> Delete(int Id)
        {
            ContactModel contactModel = await _dataContext.Contacts.FindAsync(Id);
            if (!string.Equals(contactModel.LogoImage, "null.jpg"))
            {
                string uploadsDir = Path.Combine(_webHostEnviroment.WebRootPath, "media/logo");
                string oldfilePath = Path.Combine(uploadsDir, contactModel.LogoImage);
                if (System.IO.File.Exists(oldfilePath))
                {
                    System.IO.File.Delete(oldfilePath);
                }
            }

            _dataContext.Contacts.Remove(contactModel);
            await _dataContext.SaveChangesAsync();
            TempData["Success"] = "Delete Contact Successfully!!!";
            return RedirectToAction("Index");
        }
    }
}
