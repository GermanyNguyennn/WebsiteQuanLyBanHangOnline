using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;

namespace WebsiteQuanLyBanHangOnline.Repository
{
    public class SeedData
    {       
        public static async Task SeedingDataAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<DataContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUserModel>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            context.Database.Migrate();

            // Tạo role Admin nếu chưa có
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Tạo tài khoản Admin nếu chưa có
            var adminEmail = "manhducnguyen23092003@gmail.com";
            var adminPhone = "0964429403";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new AppUserModel
                {
                    UserName = "Admin",
                    Email = adminEmail,
                    PhoneNumber = adminPhone,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "23092003"); // Mật khẩu mẫu

                if (result.Succeeded)
                {
                    // Gán quyền Admin cho tài khoản
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed dữ liệu sản phẩm nếu cần
            if (!context.Products.Any())
            {
                CategoryModel macos = new CategoryModel { Name = "MacOS", Description = "Đây là danh mục MacOS", Slug = "macos", Status = 1 };
                CategoryModel iphone = new CategoryModel { Name = "Iphone", Description = "Đây là danh mục Iphone", Slug = "iphone", Status = 1 };
                CategoryModel windows = new CategoryModel { Name = "Windows", Description = "Đây là danh mục Windows", Slug = "windows", Status = 1 };

                BrandModel apple = new BrandModel { Name = "Apple", Description = "Đây là thương hiệu Apple", Slug = "apple", Status = 1 };
                BrandModel microsoft = new BrandModel { Name = "Microsoft", Description = "Đây là thương hiệu Microsoft", Slug = "microsoft", Status = 1 };

                context.Products.AddRange(
                    new ProductModel { Name = "MacBook Air M4 15 inch 2025 10CPU 10GPU 24GB 512GB Sạc 70W", Image = "macbook-air.jpg", Description = "", Price = 42490000, Slug = "", Brand = apple, Category = macos },
                    new ProductModel { Name = "MacBook Pro 16 M4 Max 16CPU 40GPU 64GB 2TB", Image = "macbook-pro.jpg", Description = "", Price = 117490000, Slug = "", Brand = apple, Category = macos },
                    new ProductModel { Name = "iMac M4 2024 24 inch 10CPU 10GPU 24GB 512GB", Image = "imac-m4.jpg", Description = "", Price = 48990000, Slug = "", Brand = apple, Category = macos },

                    new ProductModel { Name = "iPhone 16 Pro Max 1TB", Image = "iphone-16-pro-max.jpg", Description = "", Price = 42690000, Slug = "", Brand = apple, Category = iphone },
                    new ProductModel { Name = "iPhone 16 Pro 1TB", Image = "iphone-16-pro.jpg", Description = "", Price = 38990000, Slug = "", Brand = apple, Category = iphone },
                    new ProductModel { Name = "iPhone 16 Plus 512GB", Image = "iphone-16-plus.jpg", Description = "", Price = 38990000, Slug = "", Brand = apple, Category = iphone },
                    new ProductModel { Name = "iPhone 16 512GB", Image = "iphone-16.jpg", Description = "", Price = 38990000, Slug = "", Brand = apple, Category = iphone },
                    new ProductModel { Name = "iPhone 16e 512GB", Image = "iphone-16e.jpg", Description = "", Price = 25490000, Slug = "", Brand = apple, Category = iphone }
                );

                await context.SaveChangesAsync();
            }

            if (!context.Contacts.Any())
            {
                ContactModel contactModel = new ContactModel
                {
                    Name = "Nguyễn Mạnh Đức",
                    Map = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3723.883773346561!2d105.85400031440625!3d21.00500009317313!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x3135ab4000000001%3A0x0000000000000001!2sNgõ%2084%20Phố%208%2F3%2C%20Quỳnh%20Mai%2C%20Hai%20Bà%20Trưng%2C%20Hà%20Nội!5e0!3m2!1svi!2s!4v1680000000000",
                    Email = "manhducnguyen23092003@gmail.com",
                    Phone = "0964429403"
                };

                context.Contacts.Add(contactModel);
                await context.SaveChangesAsync();
            }               
        }
    }
}
