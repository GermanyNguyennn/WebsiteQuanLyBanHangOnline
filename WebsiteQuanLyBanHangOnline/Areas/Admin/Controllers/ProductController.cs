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
            List<ProductModel> products = await _dataContext.Products.Include(p => p.Brand).Include(p => p.Category).ToListAsync();

            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }

            int count = products.Count;
            var pager = new Paginate(count, page, pageSize);
            int skip = (page - 1) * pageSize;

            var data = products.Skip(skip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);

            //return View(await _dataContext.Products.OrderBy(c => c.Id).Include(c => c.Category).Include(c => c.Brand).ToListAsync());
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
            if (ModelState.IsValid)
            {            
                productModel.Slug = productModel.Name.Replace(" ", "-");
                var slug = await _dataContext.Products.FirstOrDefaultAsync(p => p.Slug == productModel.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "Product Has In Database!!!");
                    return View(productModel);
                }
                if (productModel.ImageUpload != null)
                {
                    string upload = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                    string imageName = Guid.NewGuid().ToString() + "_" + productModel.ImageUpload.FileName;
                    string filePath = Path.Combine(upload, imageName);
                    FileStream fs = new FileStream(filePath, FileMode.Create);
                    await productModel.ImageUpload.CopyToAsync(fs);
                    fs.Close();
                    productModel.Image = imageName;
                }
                _dataContext.Add(productModel);
                await _dataContext.SaveChangesAsync();
                TempData["Success"] = "Models Are Effective!!!";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = "Models Have Some Problems!!!";
                List<string> errors = new List<string>();
                foreach(var value in ModelState.Values)
                {
                    foreach(var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }
            
            return View(productModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            ProductModel productModel = await _dataContext.Products.FindAsync(Id);
            if (productModel == null)
            {
                return RedirectToAction("Index");
            }
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

            var existed_product = _dataContext.Products.Find(productModel.Id);

            if (ModelState.IsValid)
            {
                productModel.Slug = productModel.Name.Replace(" ", "-");
                
                if (productModel.ImageUpload != null)
                {
                    
                    string upload = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                    string imageName = Guid.NewGuid().ToString() + "_" + productModel.ImageUpload.FileName;
                    string filePath = Path.Combine(upload, imageName);
                    string oldfilePath = Path.Combine(upload, existed_product.Image);
                    try
                    {
                        if (System.IO.File.Exists(oldfilePath))
                        {
                            System.IO.File.Delete(oldfilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Unable To Delete Image!!! " + ex.Message);
                    }

                    FileStream fs = new FileStream(filePath, FileMode.Create);
                    await productModel.ImageUpload.CopyToAsync(fs);
                    fs.Close();
                    existed_product.Image = imageName;                  
                }
                
                existed_product.Name = productModel.Name;
                existed_product.Description = productModel.Description;
                existed_product.Price = productModel.Price;
                existed_product.CategoryId = productModel.CategoryId;
                existed_product.BrandId = productModel.BrandId;

                _dataContext.Update(existed_product);
                await _dataContext.SaveChangesAsync();
                TempData["Success"] = "Update Product Successfully!!!";
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

            return View(productModel);
        }

        public async Task<IActionResult> Delete(int Id)
        {
            ProductModel productModel = await _dataContext.Products.FindAsync(Id);

            if (productModel == null)
            {
                return RedirectToAction("Index");
            }

            string upload = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
            string oldfilePath = Path.Combine(upload, productModel.Image);
            try
            {
                if (System.IO.File.Exists(oldfilePath))
                {
                    System.IO.File.Delete(oldfilePath);
                }
            }
            catch
            {
                ModelState.AddModelError("", "Unable To Delete Image!!!");
            }
            _dataContext.Products.Remove(productModel);
            await _dataContext.SaveChangesAsync();
            TempData["Success"] = "Delete Product Successfully!!!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> AddQuantity(int Id)
        {
            var productbyquantity = await _dataContext.ProductQuantities.Where(pq => pq.ProductId == Id).ToListAsync();
            ViewBag.ProductByQuantity = productbyquantity;
            ViewBag.ProductId = Id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(ProductQuantityModel productQuantityModel)
        {
            var product = _dataContext.Products.Find(productQuantityModel.ProductId);

            if (product == null)
            {
                return NotFound();
            }
            product.Quantity += productQuantityModel.Quantity;

            productQuantityModel.Quantity = productQuantityModel.Quantity;
            productQuantityModel.ProductId = productQuantityModel.ProductId;
            productQuantityModel.CreatedDate = DateTime.Now;


            _dataContext.Add(productQuantityModel);
            _dataContext.SaveChangesAsync();
            TempData["Success"] = "Update Quantity Successfully!!!";
            return RedirectToAction("AddQuantity", "Product", new { Id = productQuantityModel.ProductId });
        }
    }
}
