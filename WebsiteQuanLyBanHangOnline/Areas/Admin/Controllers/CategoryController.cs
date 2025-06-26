using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
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
            const int pageSize = 10;
            if (page < 1) page = 1;

            int totalItems = await _dataContext.Categories.CountAsync();
            var pager = new Paginate(totalItems, page, pageSize);

            var categories = await _dataContext.Categories
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            return View(categories);
        }

        public IActionResult Add() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CategoryModel categoryModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Dữ liệu không hợp lệ.";
                return View(categoryModel);
            }

            categoryModel.Slug = GenerateSlug(categoryModel.Name);

            bool slugExists = await _dataContext.Categories
                .AnyAsync(c => c.Slug == categoryModel.Slug);

            if (slugExists)
            {
                TempData["error"] = "Danh mục đã tồn tại.";
                return View(categoryModel);
            }

            _dataContext.Categories.Add(categoryModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Thêm danh mục thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _dataContext.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["error"] = "Không tìm thấy danh mục.";
                return RedirectToAction("Index");
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryModel categoryModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Dữ liệu không hợp lệ.";
                return View(categoryModel);
            }

            categoryModel.Slug = GenerateSlug(categoryModel.Name);

            bool slugExists = await _dataContext.Categories
                .AnyAsync(c => c.Id != categoryModel.Id && c.Slug == categoryModel.Slug);

            if (slugExists)
            {
                TempData["error"] = "Tên danh mục bị trùng với danh mục khác.";
                return View(categoryModel);
            }

            _dataContext.Update(categoryModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật danh mục thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _dataContext.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["error"] = "Không tìm thấy danh mục.";
                return RedirectToAction("Index");
            }

            _dataContext.Categories.Remove(category);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Xóa danh mục thành công!";
            return RedirectToAction("Index");
        }

        private string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            // Bước 1: Chuẩn hóa Unicode (loại bỏ dấu tiếng Việt)
            string normalized = name.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            string slug = sb.ToString().Normalize(NormalizationForm.FormC);

            // Bước 2: Chuyển sang chữ thường và loại bỏ ký tự đặc biệt
            slug = slug.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");      // chỉ giữ lại chữ, số, khoảng trắng, và -
            slug = Regex.Replace(slug, @"\s+", "-");              // thay khoảng trắng bằng dấu gạch ngang
            slug = Regex.Replace(slug, @"-+", "-");               // gộp nhiều dấu - liền nhau thành 1

            return slug.Trim('-'); // loại bỏ dấu - ở đầu/cuối
        }
    }
}
