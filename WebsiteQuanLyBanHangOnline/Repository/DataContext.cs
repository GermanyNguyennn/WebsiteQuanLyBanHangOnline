using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;

namespace WebsiteQuanLyBanHangOnline.Repository
{
    public class DataContext : IdentityDbContext<AppUserModel>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<BrandModel> Brands { get; set; }
        public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<ProductModel> Products { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderDetailModel> OrderDetails { get; set; }
        public DbSet<SliderModel> Sliders { get; set; }
        public DbSet<ContactModel> Contacts { get; set; }
        public DbSet<CompareModel> Compares { get; set; }
        public DbSet<WishlistModel> Wishlists { get; set; }
        public DbSet<ProductQuantityModel> ProductQuantities { get; set; }
        public DbSet<InformationModel> Information { get; set; }
        public DbSet<CouponModel> Coupons { get; set; }
        public DbSet<MoMoModel> MoMos { get; set; }
        public DbSet<VnPayModel> VNPays { get; set; }
    }
}
