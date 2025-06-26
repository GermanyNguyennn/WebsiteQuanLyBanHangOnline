using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class ProductController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(DataContext context, IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(1, page);
            var count = await _dataContext.Products.CountAsync();

            var pager = new Paginate(count, page, pageSize);
            var data = await _dataContext.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            return View(data);
        }

        [HttpGet]
        public IActionResult Add()
        {
            SetSelectLists();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ProductModel productModel)
        {
            SetSelectLists(productModel.CategoryId, productModel.BrandId);

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Dữ liệu không hợp lệ.";
                return View(productModel);
            }

            productModel.Slug = GenerateSlug(productModel.Name);

            if (await _dataContext.Products.AnyAsync(p => p.Slug == productModel.Slug))
            {
                TempData["error"] = "Sản phẩm đã tồn tại.";
                return View(productModel);
            }

            if (productModel.ImageUpload != null)
                productModel.Image = await SaveImageAsync(productModel.ImageUpload);

            _dataContext.Add(productModel);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Thêm sản phẩm thành công.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _dataContext.Products.FindAsync(id);
            if (product == null) return NotFound();

            SetSelectLists(product.CategoryId, product.BrandId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductModel productModel)
        {
            SetSelectLists(productModel.CategoryId, productModel.BrandId);

            var existingProduct = await _dataContext.Products.FindAsync(id);
            if (existingProduct == null) return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Dữ liệu không hợp lệ.";
                return View(productModel);
            }

            existingProduct.Slug = productModel.Name.Trim().Replace(" ", "-");

            if (productModel.ImageUpload != null)
            {
                try
                {
                    DeleteImage(existingProduct.Image);
                    existingProduct.Image = await SaveImageAsync(productModel.ImageUpload);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không thể cập nhật hình ảnh: " + ex.Message);
                    return View(productModel);
                }
            }

            existingProduct.Name = productModel.Name;
            existingProduct.Description = productModel.Description;
            existingProduct.Price = productModel.Price;
            existingProduct.CategoryId = productModel.CategoryId;
            existingProduct.BrandId = productModel.BrandId;

            _dataContext.Update(existingProduct);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật sản phẩm thành công.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _dataContext.Products.FindAsync(id);
            if (product == null) return NotFound();

            DeleteImage(product.Image);

            _dataContext.Products.Remove(product);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Xóa sản phẩm thành công.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> IndexQuantity(int id)
        {
            ViewBag.ProductByQuantity = await _dataContext.ProductQuantities
                .Where(pq => pq.ProductId == id)
                .ToListAsync();

            ViewBag.ProductId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuantity(ProductQuantityModel model)
        {
            var product = await _dataContext.Products.FindAsync(model.ProductId);
            if (product == null) return NotFound();

            product.Quantity += model.Quantity;
            model.CreatedDate = DateTime.Now;

            _dataContext.ProductQuantities.Add(model);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật số lượng thành công.";
            return RedirectToAction("IndexQuantity", new { id = model.ProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuantity(int id)
        {
            var quantity = await _dataContext.ProductQuantities.FindAsync(id);
            if (quantity == null) return NotFound();

            var product = await _dataContext.Products.FindAsync(quantity.ProductId);
            if (product == null) return NotFound();

            product.Quantity = Math.Max(0, product.Quantity - quantity.Quantity);
            _dataContext.ProductQuantities.Remove(quantity);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Xóa số lượng thành công.";
            return RedirectToAction("IndexQuantity", new { id = quantity.ProductId });
        }

        [HttpPost]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return RedirectToAction("Index");

            var products = await _dataContext.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Name.Contains(searchTerm))
                .ToListAsync();

            ViewBag.Keyword = searchTerm;
            return View(products);
        }

        private void SetSelectLists(int? categoryId = null, int? brandId = null)
        {
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", categoryId);
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", brandId);
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
            var fileName = Guid.NewGuid() + "_" + Path.GetFileName(image.FileName);
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return fileName;
        }

        private void DeleteImage(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName)) return;

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "media/products", imageName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
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
