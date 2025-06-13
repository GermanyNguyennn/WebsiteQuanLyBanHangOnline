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
        private readonly IWebHostEnvironment _webHostEnvironment;
        public SliderController(DataContext dataContext, IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = dataContext;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            int count = await _dataContext.Sliders.CountAsync();
            var pager = new Paginate(count, page, pageSize);

            var data = await _dataContext.Sliders
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            return View(data);
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
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/sliders");
                    string imageName = Guid.NewGuid() + "_" + sliderModel.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsDir, imageName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await sliderModel.ImageUpload.CopyToAsync(fs);
                    }

                    sliderModel.Image = imageName;
                }

                _dataContext.Add(sliderModel);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Slider Added Successfully!!!";
                return RedirectToAction("Index");
            }

            TempData["error"] = "Models Have Some Problems!!!";
            return BadRequest(string.Join("\n", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        public async Task<IActionResult> Edit(int Id)
        {
            var sliderModel = await _dataContext.Sliders.FindAsync(Id);
            return View(sliderModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SliderModel sliderModel)
        {
            var slider_existed = await _dataContext.Sliders.FindAsync(sliderModel.Id);
            if (slider_existed == null) return NotFound();

            if (ModelState.IsValid)
            {
                if (sliderModel.ImageUpload != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/sliders");
                    string imageName = Guid.NewGuid() + "_" + sliderModel.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsDir, imageName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await sliderModel.ImageUpload.CopyToAsync(fs);
                    }

                    slider_existed.Image = imageName;
                }

                slider_existed.Name = sliderModel.Name;
                slider_existed.Description = sliderModel.Description;
                slider_existed.Status = sliderModel.Status;

                _dataContext.Update(slider_existed);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Slider Updated Successfully!!!";
                return RedirectToAction("Index");
            }

            TempData["error"] = "Models Have Some Problems!!!";
            return BadRequest(string.Join("\n", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        public async Task<IActionResult> Delete(int Id)
        {
            var sliderModel = await _dataContext.Sliders.FindAsync(Id);
            if (sliderModel == null) return NotFound();

            if (!string.Equals(sliderModel.Image, "null.jpg", StringComparison.OrdinalIgnoreCase))
            {
                string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/sliders");
                string oldfilePath = Path.Combine(uploadsDir, sliderModel.Image);
                if (System.IO.File.Exists(oldfilePath))
                {
                    System.IO.File.Delete(oldfilePath);
                }
            }

            _dataContext.Sliders.Remove(sliderModel);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Slider Deleted Successfully!!!";
            return RedirectToAction("Index");
        }
    }
}
