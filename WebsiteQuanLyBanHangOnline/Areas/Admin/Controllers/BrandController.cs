using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Authorize(Roles = "Admin")]
    public class BrandController : Controller
    {
        private readonly DataContext _dataContext;
        public BrandController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            List<BrandModel> brands = await _dataContext.Brands.OrderBy(c => c.Id).ToListAsync();

            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }

            int count = brands.Count;
            var pager = new Paginate(count, page, pageSize);
            int skip = (page - 1) * pageSize;

            var data = brands.Skip(skip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);

            //return View(await _dataContext.Brands.OrderBy(c => c.Id).ToListAsync());
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(BrandModel brandModel)
        {
            if (ModelState.IsValid)
            {
                brandModel.Slug = brandModel.Name.Replace(" ", "-");
                var slug = await _dataContext.Products.FirstOrDefaultAsync(p => p.Slug == brandModel.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "Brand Has In Database!!!");
                    return View(brandModel);
                }

                _dataContext.Add(brandModel);
                await _dataContext.SaveChangesAsync();
                TempData["Success"] = "Models Are Effective!!!";
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

            return View(brandModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            BrandModel brandModel = await _dataContext.Brands.FindAsync(Id);
            return View(brandModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BrandModel brandModel)
        {
            if (ModelState.IsValid)
            {
                brandModel.Slug = brandModel.Name.Replace(" ", "-");

                _dataContext.Update(brandModel);
                await _dataContext.SaveChangesAsync();
                TempData["Success"] = "Models Are Effective!!!";
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

            return View(brandModel);
        }

        public async Task<IActionResult> Delete(int Id)
        {
            BrandModel brandModel = await _dataContext.Brands.FindAsync(Id);

            _dataContext.Brands.Remove(brandModel);
            await _dataContext.SaveChangesAsync();
            TempData["Success"] = "Delete Brand Successfully!!!";
            return RedirectToAction("Index");
        }
    }
}
