using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly DataContext _dataContext;
        public CategoryController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            List<CategoryModel> categories = await _dataContext.Categories.OrderBy(c => c.Id).ToListAsync();

            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }

            int count = categories.Count;
            var pager = new Paginate(count, page, pageSize);
            int skip = (page - 1) * pageSize;

            var data = categories.Skip(skip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);

            //return View(await _dataContext.Categories.OrderBy(c => c.Id).ToListAsync());
        }

        public IActionResult Add()
        {         
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CategoryModel categoryModel)
        {
            if (ModelState.IsValid)
            {
                categoryModel.Slug = categoryModel.Name.Replace(" ", "-");
                var slug = await _dataContext.Products.FirstOrDefaultAsync(p => p.Slug == categoryModel.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "Category Has In Database!!!");
                    return View(categoryModel);
                }
                
                _dataContext.Add(categoryModel);
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

            return View(categoryModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            CategoryModel categoryModel = await _dataContext.Categories.FindAsync(Id);       
            return View(categoryModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryModel categoryModel)
        {
            if (ModelState.IsValid)
            {
                categoryModel.Slug = categoryModel.Name.Replace(" ", "-");
                
                _dataContext.Update(categoryModel);
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

            return View(categoryModel);
        }

        public async Task<IActionResult> Delete(int Id)
        {
            CategoryModel categoryModel = await _dataContext.Categories.FindAsync(Id);
            
            _dataContext.Categories.Remove(categoryModel);
            await _dataContext.SaveChangesAsync();
            TempData["Success"] = "Delete Category Successfully!!!";
            return RedirectToAction("Index");
        }
    }
}
