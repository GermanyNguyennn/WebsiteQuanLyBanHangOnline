using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleController(RoleManager<IdentityRole> roleManager, DataContext dataContext)
        {
            _roleManager = roleManager;
            _dataContext = dataContext;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            int count = await _dataContext.Roles.CountAsync();
            var pager = new Paginate(count, page, pageSize);

            var roles = await _dataContext.Roles
                .OrderByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            return View(roles);
        }


        [HttpGet]
        public IActionResult Add()
        {
            return View(new IdentityRole());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(IdentityRole model)
        {
            if (string.IsNullOrWhiteSpace(model?.Name))
            {
                TempData["error"] = "Tên vai trò không được để trống.";
                return View(model);
            }

            if (await _roleManager.RoleExistsAsync(model.Name))
            {
                TempData["error"] = "Vai trò đã tồn tại.";
                return View(model);
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
            if (result.Succeeded)
            {
                TempData["success"] = "Thêm vai trò thành công.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, IdentityRole model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Dữ liệu không hợp lệ.";
                return View(model);
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            role.Name = model.Name;

            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                TempData["success"] = "Cập nhật vai trò thành công.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["success"] = "Xóa vai trò thành công.";
            }
            else
            {
                TempData["error"] = "Không thể xóa vai trò.";
            }

            return RedirectToAction("Index");
        }
    }
}
