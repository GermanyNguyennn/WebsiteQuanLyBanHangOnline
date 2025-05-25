using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;

namespace WebsiteQuanLyBanHangOnline.Repository
{
    public class SeedData
    {
        public static void SeedingData(DataContext _context)
        {
            _context.Database.Migrate();
            if (!_context.Products.Any())
            {
                CategoryModel macos = new CategoryModel { Name = "MacOS", Description = "Đây là danh mục MacOS", Slug = "macos" , Status = 1};
                CategoryModel iphone = new CategoryModel { Name = "Iphone", Description = "Đây là danh mục Iphone", Slug = "iphone", Status = 1 };
                CategoryModel windows = new CategoryModel { Name = "Windows", Description = "Đây là danh mục Windows", Slug = "windows", Status = 1 };
                BrandModel apple = new BrandModel { Name = "Apple", Description = "Đây là thương hiệu Apple", Slug = "apple", Status = 1 };
                BrandModel microsoft = new BrandModel { Name = "Microsoft", Description = "Đây là thương hiệu Microsoft", Slug = "microsoft", Status = 1 };

                _context.Products.AddRange (
                    new ProductModel { Name = "MacBook Air M4 15 inch 2025 10CPU 10GPU 24GB 512GB Sạc 70W", Image = "macbook-air.jpg", Description = "123", Price = 1637, Slug = "", Brand = apple, Category = macos },
                    new ProductModel { Name = "MacBook Pro 16 M4 Max 16CPU 40GPU 64GB 2TB", Image = "macbook-pro.jpg", Description = "456", Price = 4527, Slug = "", Brand = apple, Category = macos },

                    new ProductModel { Name = "iPhone 16 Pro Max 1TB", Image = "iphone-16-pro-max.jpg", Description = "135", Price = 1645, Slug = "", Brand = apple, Category = iphone },
                    new ProductModel { Name = "iPhone 16 Pro 1TB", Image = "iphone-16-pro.jpg", Description = "246", Price = 1538, Slug = "", Brand = apple, Category = iphone }
                );
                _context.SaveChanges();
            }
        }
    }
}
