using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebsiteQuanLyBanHangOnline.Repository.Components
{
    public class ProductsViewComponent : ViewComponent
    {
        private readonly DataContext _dataContext;
        public ProductsViewComponent(DataContext context)
        {
            _dataContext = context;
        }

        public async Task<IViewComponentResult> InvokeAsync() => View(await _dataContext.Products.ToListAsync());
    }
}
