using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using WebsiteQuanLyBanHangOnline.Models;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;
using WebsiteQuanLyBanHangOnline.Repository;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Authorize(Roles = "Admin, Employee")]
    public class UserController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _dataContext;

        public UserController(DataContext dataContext, UserManager<AppUserModel> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dataContext = dataContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usersWithRoles = await (from u in _dataContext.Users
                                        join ur in _dataContext.UserRoles on u.Id equals ur.UserId
                                        join r in _dataContext.Roles on ur.RoleId equals r.Id
                                        select new UserWithRoleViewModel
                                        {
                                            User = u,
                                            RoleName = r.Name
                                        }).ToListAsync();

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
        public async Task<IActionResult> Add(AppUserModel appUserModel, string selectedRoleId)
        {
            if (ModelState.IsValid)
            {
                var createUserResult = await _userManager.CreateAsync(appUserModel, appUserModel.PasswordHash);
                if (createUserResult.Succeeded)
                {
                    var role = await _roleManager.FindByIdAsync(selectedRoleId);
                    if (role != null)
                    {
                        var addToRoleResult = await _userManager.AddToRoleAsync(appUserModel, role.Name);
                        if (!addToRoleResult.Succeeded)
                        {
                            foreach (var error in addToRoleResult.Errors)
                                ModelState.AddModelError(string.Empty, error.Description);

                            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name", selectedRoleId);
                            return View(appUserModel);
                        }
                    }

                    TempData["success"] = "User Added Successfully!!!";
                    return RedirectToAction("Index");
                }

                foreach (var error in createUserResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name", selectedRoleId);
            return View(appUserModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoleName = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            var currentRole = currentRoleName == null ? null : await _roleManager.FindByNameAsync(currentRoleName);

            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Id", "Name", currentRole?.Id);
            ViewBag.CurrentRoleId = currentRole?.Id;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, AppUserModel appUserModel, string selectedRoleId)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                user.UserName = appUserModel.UserName;
                user.Email = appUserModel.Email;
                user.PhoneNumber = appUserModel.PhoneNumber;

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                var newRole = await _roleManager.FindByIdAsync(selectedRoleId);
                if (newRole != null)
                    await _userManager.AddToRoleAsync(user, newRole.Name);

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["success"] = "User Updated Successfully!!!";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Id", "Name", selectedRoleId);
            return View(appUserModel);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded) return View("Error");

            TempData["success"] = "User Deleted Successfully!!!";
            return RedirectToAction("Index");
        }
    }
}
