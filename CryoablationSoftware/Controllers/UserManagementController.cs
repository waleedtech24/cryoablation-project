using CryoablationSoftware.Data;
using CryoablationSoftware.Services;
using CryoablationSoftware.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryoablationSoftware.Controllers
{
    [Authorize(Roles = "Super Admin,SuperAdmin2")]
    public class UserManagementController : BaseController
    {
        private readonly IUserManagementService _userService;
        private readonly ApplicationDbContext _context;
        private readonly ICurrentSiteService _currentSiteService;
        public UserManagementController(
            IUserManagementService userService,
            ApplicationDbContext context, ICurrentSiteService currentSiteService)
            : base(currentSiteService, context)
        {
            _userService = userService;
            _context = context;
            _currentSiteService = currentSiteService;
        }

        // GET: /UserManagement/Index
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        // GET: /UserManagement/Create
        public async Task<IActionResult> Create()
        {
            var model = new UserFormViewModel
            {
                AvailableSites = await GetSitesDropdownAsync()
            };
            return View(model);
        }

        // POST: /UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            // Password Create pe required hai
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "Password is required when creating a user.");

            if (!ModelState.IsValid)
            {
                model.AvailableSites = await GetSitesDropdownAsync();
                return View(model);
            }

            var (success, message) = await _userService.CreateUserAsync(model);

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = message;
            model.AvailableSites = await GetSitesDropdownAsync();
            return View(model);
        }

        // GET: /UserManagement/Edit/{userId}
        public async Task<IActionResult> Edit(string id)
        {
            var model = await _userService.GetUserForEditAsync(id);
            if (model == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            model.AvailableSites = await GetSitesDropdownAsync();
            return View(model);
        }

        // POST: /UserManagement/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserFormViewModel model)
        {
            // Password edit pe optional hai
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                model.AvailableSites = await GetSitesDropdownAsync();
                return View(model);
            }

            var (success, message) = await _userService.UpdateUserAsync(model);

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = message;
            model.AvailableSites = await GetSitesDropdownAsync();
            return View(model);
        }

        // POST: /UserManagement/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var (success, message) = await _userService.ToggleUserStatusAsync(id);

            if (success)
                TempData["SuccessMessage"] = message;
            else
                TempData["ErrorMessage"] = message;

            return RedirectToAction(nameof(Index));
        }

        // GET: /UserManagement/ResetPassword/{userId}
        public async Task<IActionResult> ResetPassword(string id)
        {
            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(new ResetPasswordViewModel
            {
                UserId = id,
                FullName = profile.FullName
            });
        }

        // POST: /UserManagement/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, message) = await _userService.ResetPasswordAsync(model.UserId, model.NewPassword);

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = message;
            return View(model);
        }

        // Helper - Sites dropdown
        private async Task<List<SiteDropdownItem>> GetSitesDropdownAsync()
        {
            return await _context.Sites
                .Where(s => s.IsActive)
                .OrderBy(s => s.SiteName)
                .Select(s => new SiteDropdownItem
                {
                    SiteId = s.SiteId,
                    SiteName = s.SiteName,
                    SiteCity = s.SiteCity ?? ""
                })
                .ToListAsync();
        }
    }
}