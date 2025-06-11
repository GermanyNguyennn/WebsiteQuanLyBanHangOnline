using Azure;
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
            var products = await _dataContext.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .ToListAsync();

            page = Math.Max(page, 1);
            var count = products.Count;
            var pager = new Paginate(count, page, pageSize);
            var data = products.Skip((page - 1) * pageSize).Take(pager.PageSize).ToList();

            ViewBag.Pager = pager;
            return View(data);
        }

        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name");
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ProductModel productModel)
        {
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", productModel.CategoryId);
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", productModel.BrandId);

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Models Have Some Problems!!!";
                return BadRequest(string.Join("\n", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            productModel.Slug = productModel.Name.Replace(" ", "-");
            if (await _dataContext.Products.AnyAsync(p => p.Slug == productModel.Slug))
            {
                ModelState.AddModelError("", "Product Has In Database!!!");
                return View(productModel);
            }

            if (productModel.ImageUpload != null)
            {
                var upload = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                var imageName = Guid.NewGuid() + "_" + productModel.ImageUpload.FileName;
                var filePath = Path.Combine(upload, imageName);

                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    await productModel.ImageUpload.CopyToAsync(fs);
                }

                productModel.Image = imageName;
            }

            _dataContext.Add(productModel);
            await _dataContext.SaveChangesAsync();
            TempData["Success"] = "Product Added Successfully!!!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var productModel = await _dataContext.Products.FindAsync(Id);
            if (productModel == null) return RedirectToAction("Index");

            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", productModel.CategoryId);
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", productModel.BrandId);
            return View(productModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, ProductModel productModel)
        {
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", productModel.CategoryId);
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", productModel.BrandId);

            var existingProduct = await _dataContext.Products.FindAsync(productModel.Id);
            if (existingProduct == null) return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Models Have Some Problems!!!";
                return BadRequest(string.Join("\n", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            existingProduct.Slug = productModel.Name.Replace(" ", "-");

            if (productModel.ImageUpload != null)
            {
                var upload = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                var imageName = Guid.NewGuid() + "_" + productModel.ImageUpload.FileName;
                var newFilePath = Path.Combine(upload, imageName);
                var oldFilePath = Path.Combine(upload, existingProduct.Image);

                try
                {
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Unable To Delete Image!!! " + ex.Message);
                    return View(productModel);
                }

                using (var fs = new FileStream(newFilePath, FileMode.Create))
                {
                    await productModel.ImageUpload.CopyToAsync(fs);
                }

                existingProduct.Image = imageName;
            }

            existingProduct.Name = productModel.Name;
            existingProduct.Description = productModel.Description;
            existingProduct.Price = productModel.Price;
            existingProduct.CategoryId = productModel.CategoryId;
            existingProduct.BrandId = productModel.BrandId;

            _dataContext.Update(existingProduct);
            await _dataContext.SaveChangesAsync();

            TempData["Success"] = "Product Updated Successfully!!!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int Id)
        {
            var productModel = await _dataContext.Products.FindAsync(Id);
            if (productModel == null) return RedirectToAction("Index");

            var upload = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
            var filePath = Path.Combine(upload, productModel.Image);

            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch
            {
                ModelState.AddModelError("", "Unable To Delete Image!!!");
            }

            _dataContext.Products.Remove(productModel);
            await _dataContext.SaveChangesAsync();

            TempData["Success"] = "Product Deleted Successfully!!!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> AddQuantity(int Id)
        {
            ViewBag.ProductByQuantity = await _dataContext.ProductQuantities
                .Where(pq => pq.ProductId == Id)
                .ToListAsync();

            ViewBag.ProductId = Id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(ProductQuantityModel productQuantityModel)
        {
            var product = await _dataContext.Products.FindAsync(productQuantityModel.ProductId);
            if (product == null) return NotFound();

            product.Quantity += productQuantityModel.Quantity;
            productQuantityModel.CreatedDate = DateTime.Now;

            _dataContext.Add(productQuantityModel);
            await _dataContext.SaveChangesAsync();

            TempData["Success"] = "Quantity Updated Successfully!!!";
            return RedirectToAction("AddQuantity", new { Id = productQuantityModel.ProductId });
        }
    }
}
