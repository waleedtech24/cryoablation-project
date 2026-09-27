using CryoablationSoftware.Services;
using CryoablationSoftware.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryoablationSoftware.Controllers
{
    [Authorize(Roles = "Super Admin")]
    public class SuperAdminController : Controller
    {
        private readonly ISuperAdminService _superAdminService;
        private readonly ICurrentSiteService _currentSiteService;
        public SuperAdminController(ISuperAdminService superAdminService, ICurrentSiteService currentSiteService)
        {
            _superAdminService = superAdminService;
            _currentSiteService = currentSiteService;
        }

        // GET: /SuperAdmin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Wapis Dashboard pe aao toh selection clear karo
                _currentSiteService.SetSelectedSite(null);
                var model = await _superAdminService.GetDashboardAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading dashboard: {ex.Message}";
                return View(new SuperAdminDashboardViewModel());
            }
        }

        [HttpPost]
        public IActionResult SelectSite(int? siteId, string returnUrl = null)
        {
            _currentSiteService.SetSelectedSite(siteId);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Dashboard");
        }

        // GET: /SuperAdmin/SiteDetail/5
        public async Task<IActionResult> SiteDetail(int id)
        {
            try
            {
                var model = await _superAdminService.GetSiteDetailAsync(id);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Site not found.";
                    return RedirectToAction("Dashboard");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading site detail: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: /SuperAdmin/ComparativeAnalytics
        public async Task<IActionResult> ComparativeAnalytics()
        {
            try
            {
                var model = await _superAdminService.GetComparativeAnalyticsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading analytics: {ex.Message}";
                return View(new ComparativeAnalyticsViewModel());
            }
        }

        // GET: /SuperAdmin/PerformanceMetrics
        public async Task<IActionResult> PerformanceMetrics()
        {
            try
            {
                var model = await _superAdminService.GetPerformanceMetricsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading metrics: {ex.Message}";
                return View(new PerformanceMetricsViewModel());
            }
        }

        // GET: /SuperAdmin/ConsolidatedReports
        public async Task<IActionResult> ConsolidatedReports()
        {
            try
            {
                var model = await _superAdminService.GetConsolidatedReportsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading reports: {ex.Message}";
                return View(new ConsolidatedReportsViewModel());
            }
        }
    }
}
