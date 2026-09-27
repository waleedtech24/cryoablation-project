using CryoablationSoftware.Data;
using CryoablationSoftware.Services;
using CryoablationSoftware.ViewModels;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryoablationSoftware.Controllers
{
    [Authorize(Roles = "SuperAdmin2")]
    public class SiteController : BaseController
    {
        private readonly ISiteService _siteService;
        private readonly ICurrentSiteService _currentSiteService;
        private readonly ApplicationDbContext _context;
        public SiteController(ISiteService siteService, ICurrentSiteService currentSiteService, ApplicationDbContext context)
            : base(currentSiteService, context)
        {
            _siteService = siteService;
            _currentSiteService = currentSiteService;
        }

        // GET: /Site/Index
        public async Task<IActionResult> Index()
        {
            var sites = await _siteService.GetAllSitesAsync();
            return View(sites);
        }

        // GET: /Site/Create
        public IActionResult Create()
        {
            return View(new SiteFormViewModel());
        }

        // POST: /Site/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SiteFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, message) = await _siteService.CreateSiteAsync(model);

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = message;
            return View(model);
        }

        // GET: /Site/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _siteService.GetSiteForEditAsync(id);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Site not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // POST: /Site/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SiteFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, message) = await _siteService.UpdateSiteAsync(model);

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = message;
            return View(model);
        }

        // POST: /Site/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var (success, message) = await _siteService.ToggleSiteStatusAsync(id);

            if (success)
                TempData["SuccessMessage"] = message;
            else
                TempData["ErrorMessage"] = message;

            return RedirectToAction(nameof(Index));
        }
    }
}