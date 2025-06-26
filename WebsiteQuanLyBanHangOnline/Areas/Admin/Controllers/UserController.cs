using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, Employee")]
    public class UserController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _dataContext;

        public UserController(DataContext dataContext, UserManager<AppUserModel> userManager, RoleManager<IdentityRole> roleManager)
        {
            _dataContext = dataContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var usersWithRolesQuery = from u in _dataContext.Users
                                      join ur in _dataContext.UserRoles on u.Id equals ur.UserId
                                      join r in _dataContext.Roles on ur.RoleId equals r.Id
                                      select new UserWithRoleViewModel
                                      {
                                          User = u,
                                          RoleName = r.Name
                                      };

            int count = await usersWithRolesQuery.CountAsync();
            var pager = new Paginate(count, page, pageSize);

            var usersWithRoles = await usersWithRolesQuery
                .OrderByDescending(x => x.User.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Pager = pager;
            return View(usersWithRoles);
        }


        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name");
            return View(new AppUserModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AppUserModel model, string selectedRoleId)
        {
            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name", selectedRoleId);

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Dữ liệu không hợp lệ.";
                return View(model);
            }

            var createResult = await _userManager.CreateAsync(model, model.PasswordHash);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            var role = await _roleManager.FindByIdAsync(selectedRoleId);
            if (role != null)
            {
                var roleResult = await _userManager.AddToRoleAsync(model, role.Name);
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(model);
                }
            }

            TempData["success"] = "Thêm người dùng thành công.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoleName = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            var currentRole = currentRoleName != null ? await _roleManager.FindByNameAsync(currentRoleName) : null;

            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name", currentRole?.Id);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, AppUserModel model, string selectedRoleId)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name", selectedRoleId);

            if (!ModelState.IsValid) return View(model);

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            // Cập nhật role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var role = await _roleManager.FindByIdAsync(selectedRoleId);
            if (role != null)
                await _userManager.AddToRoleAsync(user, role.Name);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            TempData["success"] = "Cập nhật người dùng thành công.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                TempData["error"] = "Không thể xoá người dùng.";
                return RedirectToAction("Index");
            }

            TempData["success"] = "Xoá người dùng thành công.";
            return RedirectToAction("Index");
        }
    }
}
