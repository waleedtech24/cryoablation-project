using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModels;

namespace CryoablationSoftware.Services
{
    public interface ISiteService
    {
        Task<List<SiteListViewModel>> GetAllSitesAsync();
        Task<SiteFormViewModel> GetSiteForEditAsync(int siteId);
        Task<(bool Success, string Message)> CreateSiteAsync(SiteFormViewModel model);
        Task<(bool Success, string Message)> UpdateSiteAsync(SiteFormViewModel model);
        Task<(bool Success, string Message)> ToggleSiteStatusAsync(int siteId);
        Task<Site?> GetSiteByIdAsync(int siteId);
    }
}