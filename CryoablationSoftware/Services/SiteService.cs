using CryoablationSoftware.Data;
using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CryoablationSoftware.Services
{
    public class SiteService : ISiteService
    {
        private readonly ApplicationDbContext _context;

        public SiteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SiteListViewModel>> GetAllSitesAsync()
        {
            return await _context.Sites
                .OrderBy(s => s.SiteName)
                .Select(s => new SiteListViewModel
                {
                    SiteId = s.SiteId,
                    SiteName = s.SiteName,
                    SiteCity = s.SiteCity,
                    SitePhone = s.SitePhone,
                    SiteEmail = s.SiteEmail,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    TotalUsers = _context.UserProfiles.Count(u => u.SiteId == s.SiteId && u.IsActive),
                    TotalPatients = _context.Patients.Count(p => p.SiteId == s.SiteId && p.IsActive)
                })
                .ToListAsync();
        }

        public async Task<SiteFormViewModel> GetSiteForEditAsync(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null) return null;

            return new SiteFormViewModel
            {
                SiteId = site.SiteId,
                SiteName = site.SiteName,
                SiteCity = site.SiteCity,
                SiteAddress = site.SiteAddress,
                SitePhone = site.SitePhone,
                SiteEmail = site.SiteEmail,
                SiteDescription = site.SiteDescription,
                IsActive = site.IsActive
            };
        }

        public async Task<(bool Success, string Message)> CreateSiteAsync(SiteFormViewModel model)
        {
            try
            {
                // Duplicate name check
                var exists = await _context.Sites
                    .AnyAsync(s => s.SiteName.ToLower() == model.SiteName.ToLower());

                if (exists)
                    return (false, $"Site '{model.SiteName}' already exists.");

                var site = new Site
                {
                    SiteName = model.SiteName.Trim(),
                    SiteCity = model.SiteCity?.Trim(),
                    SiteAddress = model.SiteAddress?.Trim(),
                    SitePhone = model.SitePhone?.Trim(),
                    SiteEmail = model.SiteEmail?.Trim(),
                    SiteDescription = model.SiteDescription?.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Sites.Add(site);
                await _context.SaveChangesAsync();

                return (true, $"Site '{site.SiteName}' created successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating site: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateSiteAsync(SiteFormViewModel model)
        {
            try
            {
                var site = await _context.Sites.FindAsync(model.SiteId);
                if (site == null)
                    return (false, "Site not found.");

                // Duplicate name check (exclude self)
                var exists = await _context.Sites
                    .AnyAsync(s => s.SiteName.ToLower() == model.SiteName.ToLower()
                                && s.SiteId != model.SiteId);

                if (exists)
                    return (false, $"Another site with name '{model.SiteName}' already exists.");

                site.SiteName = model.SiteName.Trim();
                site.SiteCity = model.SiteCity?.Trim();
                site.SiteAddress = model.SiteAddress?.Trim();
                site.SitePhone = model.SitePhone?.Trim();
                site.SiteEmail = model.SiteEmail?.Trim();
                site.SiteDescription = model.SiteDescription?.Trim();

                await _context.SaveChangesAsync();

                return (true, $"Site '{site.SiteName}' updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating site: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleSiteStatusAsync(int siteId)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null)
                    return (false, "Site not found.");

                site.IsActive = !site.IsActive;
                await _context.SaveChangesAsync();

                var status = site.IsActive ? "activated" : "deactivated";
                return (true, $"Site '{site.SiteName}' {status} successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating site status: {ex.Message}");
            }
        }

        public async Task<Site?> GetSiteByIdAsync(int siteId)
        {
            return await _context.Sites.FindAsync(siteId);
        }
    }
}