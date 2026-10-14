using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        // GET: Users
        public async Task<IActionResult> Index(string? search, string? roleFilter)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u => u.Email!.ToLower().Contains(s) || u.FullName.ToLower().Contains(s));
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var userViewModels = new List<UserItemViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var primaryRole = userRoles.FirstOrDefault() ?? "None";

                if (!string.IsNullOrWhiteSpace(roleFilter) && !primaryRole.Equals(roleFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                userViewModels.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    Role = primaryRole,
                    IsActive = user.IsActive,
                    IsLockedOut = await _userManager.IsLockedOutAsync(user),
                    FailedLoginCount = user.AccessFailedCount,
                    LockoutEnd = user.LockoutEnd,
                    CreatedDate = user.CreatedDate
                });
            }

            var vm = new UserManagementIndexViewModel
            {
                Users = userViewModels,
                SearchTerm = search,
                RoleFilter = roleFilter
            };

            return View(vm);
        }

        // GET: Users/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "SalesExecutive",
                IsActive = user.IsActive
            };

            ViewBag.AvailableRoles = new[] { "Admin", "Manager", "SalesExecutive" };
            return View(model);
        }

        // POST: Users/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableRoles = new[] { "Admin", "Manager", "SalesExecutive" };
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.IsActive = model.IsActive;
            await _userManager.UpdateAsync(user);

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                module: "UserManagement",
                action: "Role Change",
                entityName: "User",
                recordId: user.Id,
                oldValue: string.Join(", ", currentRoles),
                newValue: model.Role,
                result: "Success",
                details: $"Updated user {user.Email}: role set to {model.Role}, active: {model.IsActive}."
            );

            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ToggleActive/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(
                module: "UserManagement",
                action: user.IsActive ? "Activate" : "Deactivate",
                entityName: "User",
                recordId: user.Id,
                result: "Success",
                details: $"User {user.Email} status set to {(user.IsActive ? "Active" : "Inactive")}."
            );

            TempData["SuccessMessage"] = $"User {(user.IsActive ? "activated" : "deactivated")} successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/Unlock/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogAsync(
                module: "UserManagement",
                action: "Unlock",
                entityName: "User",
                recordId: user.Id,
                result: "Success",
                details: $"User {user.Email} unlocked by administrator."
            );

            TempData["SuccessMessage"] = $"Account for {user.Email} has been unlocked!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/ResetPassword/id
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(new ResetPasswordAdminViewModel
            {
                UserId = user.Id,
                UserName = user.Email ?? ""
            });
        }

        // POST: Users/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                await _auditService.LogAsync(
                    module: "Security",
                    action: "Password Reset",
                    entityName: "User",
                    recordId: user.Id,
                    result: "Success",
                    details: $"Administrator reset password for user {user.Email}."
                );

                TempData["SuccessMessage"] = $"Password for {user.Email} reset successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}
