using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SliderController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnviroment;
        public SliderController(DataContext dataContext, IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = dataContext;
            _webHostEnviroment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            List<SliderModel> sliders = await _dataContext.Sliders.OrderBy(c => c.Id).ToListAsync();

            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }

            int count = sliders.Count;
            var pager = new Paginate(count, page, pageSize);
            int skip = (page - 1) * pageSize;

            var data = sliders.Skip(skip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);

            //return View(await _dataContext.Sliders.OrderBy(p => p.Id).ToListAsync());
        }
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(SliderModel sliderModel)
        {

            if (ModelState.IsValid)
            {

                if (sliderModel.ImageUpload != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnviroment.WebRootPath, "media/sliders");
                    string imageName = Guid.NewGuid().ToString() + "_" + sliderModel.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsDir, imageName);

                    FileStream fs = new FileStream(filePath, FileMode.Create);
                    await sliderModel.ImageUpload.CopyToAsync(fs);
                    fs.Close();
                    sliderModel.Image = imageName;
                }

                _dataContext.Add(sliderModel);
                await _dataContext.SaveChangesAsync();
                TempData["Success"] = "Add Slider Successfully!!!";
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
            return View(sliderModel);
        }

        public async Task<IActionResult> Edit(int Id)
        {
            SliderModel sliderModel = await _dataContext.Sliders.FindAsync(Id);

            return View(sliderModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SliderModel sliderModel)
        {
            var slider_existed = _dataContext.Sliders.Find(sliderModel.Id);
            if (ModelState.IsValid)
            {

                if (sliderModel.ImageUpload != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnviroment.WebRootPath, "media/sliders");
                    string imageName = Guid.NewGuid().ToString() + "_" + sliderModel.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsDir, imageName);

                    FileStream fs = new FileStream(filePath, FileMode.Create);
                    await sliderModel.ImageUpload.CopyToAsync(fs);
                    fs.Close();
                    slider_existed.Image = imageName;
                }
                slider_existed.Name = sliderModel.Name;
                slider_existed.Description = sliderModel.Description;
                slider_existed.Status = sliderModel.Status;


                _dataContext.Update(slider_existed);
                await _dataContext.SaveChangesAsync();
                TempData["Success"] = "Update Slider Successfully!!!";
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
            return View(sliderModel);
        }

        public async Task<IActionResult> Delete(int Id)
        {
            SliderModel sliderModel = await _dataContext.Sliders.FindAsync(Id);
            if (!string.Equals(sliderModel.Image, "null.jpg"))
            {
                string uploadsDir = Path.Combine(_webHostEnviroment.WebRootPath, "media/sliders");
                string oldfilePath = Path.Combine(uploadsDir, sliderModel.Image);
                if (System.IO.File.Exists(oldfilePath))
                {
                    System.IO.File.Delete(oldfilePath);
                }
            }

            _dataContext.Sliders.Remove(sliderModel);
            await _dataContext.SaveChangesAsync();
            TempData["Success"] = "Delete Slider Successfully!!!";
            return RedirectToAction("Index");
        }
    }
}
