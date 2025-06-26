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
    public class BrandController : Controller
    {
        private readonly DataContext _dataContext;

        public BrandController(DataContext context)
        {
            _dataContext = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            int totalItems = await _dataContext.Brands.CountAsync();
            var pager = new Paginate(totalItems, page, pageSize);

            var brands = await _dataContext.Brands
                .OrderBy(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            return View(brands);
        }

        public IActionResult Add() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(BrandModel brandModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Dữ liệu không hợp lệ.";
                return View(brandModel);
            }

            brandModel.Slug = GenerateSlug(brandModel.Name);

            bool slugExists = await _dataContext.Brands
                .AnyAsync(b => b.Slug == brandModel.Slug);

            if (slugExists)
            {
                TempData["error"] = "Thương hiệu đã tồn tại.";
                return View(brandModel);
            }

            _dataContext.Brands.Add(brandModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Thêm thương hiệu thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _dataContext.Brands.FindAsync(id);
            if (brand == null)
            {
                TempData["error"] = "Không tìm thấy thương hiệu.";
                return RedirectToAction("Index");
            }

            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BrandModel brandModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Dữ liệu không hợp lệ.";
                return View(brandModel);
            }

            brandModel.Slug = GenerateSlug(brandModel.Name);

            bool slugExists = await _dataContext.Brands
                .AnyAsync(b => b.Id != brandModel.Id && b.Slug == brandModel.Slug);

            if (slugExists)
            {
                TempData["error"] = "Tên thương hiệu bị trùng với thương hiệu khác.";
                return View(brandModel);
            }

            _dataContext.Update(brandModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật thương hiệu thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _dataContext.Brands.FindAsync(id);
            if (brand == null)
            {
                TempData["error"] = "Không tìm thấy thương hiệu.";
                return RedirectToAction("Index");
            }

            _dataContext.Brands.Remove(brand);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Xóa thương hiệu thành công!";
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
