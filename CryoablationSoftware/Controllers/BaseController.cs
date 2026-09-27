using CryoablationSoftware.Data;
using CryoablationSoftware.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace CryoablationSoftware.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ICurrentSiteService _currentSiteService;
        private readonly ApplicationDbContext _context;

        public BaseController(ICurrentSiteService currentSiteService, ApplicationDbContext context)
        {
            _currentSiteService = currentSiteService;
            _context = context;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var isSuperAdmin = await _currentSiteService.IsSuperAdminAsync();
            ViewBag.IsSuperAdmin = isSuperAdmin;
            ViewBag.CurrentSiteName = await _currentSiteService.GetCurrentSiteNameAsync();

            // Super Admin ke liye navbar dropdown data
            if (isSuperAdmin)
            {
                ViewBag.AllSites = await _context.Sites
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SiteName)
                    .ToListAsync();

                ViewBag.SelectedSiteId = _currentSiteService.GetSelectedSiteFromSession();
            }

            await next();
        }
    }
}
