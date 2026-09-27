using CryoablationSoftware.Data;
using Microsoft.EntityFrameworkCore;
 
namespace CryoablationSoftware.Services
{
    public interface ICurrentSiteService
    {
        Task<int?> GetCurrentSiteIdAsync();
        Task<bool> IsSuperAdminAsync();
        Task<string?> GetCurrentSiteNameAsync();
        string? GetCurrentUserId();
 
        // Super Admin ke liye — session mein selected site store karo
        void SetSelectedSite(int? siteId);
        int? GetSelectedSiteFromSession();
    }
 
    public class CurrentSiteService : ICurrentSiteService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
 
        private const string SessionKey = "SuperAdmin_SelectedSiteId";
 
        // Per-request cache
        private int? _cachedSiteId;
        private bool _cacheLoaded = false;
 
        public CurrentSiteService(
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }
 
        public string? GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated)
                return null;
            return user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }
 
        public async Task<bool> IsSuperAdminAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return false;
            return user.IsInRole("Super Admin");
        }
 
        // ── Session mein site ID store karo (Super Admin only) ──
        public void SetSelectedSite(int? siteId)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;
 
            if (siteId.HasValue)
                session.SetInt32(SessionKey, siteId.Value);
            else
                session.Remove(SessionKey);
        }
 
        // ── Session se selected site ID padho ──
        public int? GetSelectedSiteFromSession()
        {
            return _httpContextAccessor.HttpContext?.Session?.GetInt32(SessionKey);
        }
 
        // ── Main method — sab jagah inject ho ke use hota hai ──
        public async Task<int?> GetCurrentSiteIdAsync()
        {
            if (await IsSuperAdminAsync())
            {
                // Super Admin ke liye session check karo
                // Session mein site selected hai → us site ka data
                // Session empty hai → null → sab sites ka data
                return GetSelectedSiteFromSession();
            }
 
            // Normal user — cache check
            if (_cacheLoaded)
                return _cachedSiteId;
 
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _cacheLoaded = true;
                return null;
            }
 
            var profile = await _context.UserProfiles
                .Where(u => u.UserId == userId && u.IsActive)
                .Select(u => new { u.SiteId })
                .FirstOrDefaultAsync();
 
            _cachedSiteId = profile?.SiteId;
            _cacheLoaded = true;
 
            return _cachedSiteId;
        }
 
        public async Task<string?> GetCurrentSiteNameAsync()
        {
            var siteId = await GetCurrentSiteIdAsync();
            if (siteId == null) return null;
 
            return await _context.Sites
                .Where(s => s.SiteId == siteId && s.IsActive)
                .Select(s => s.SiteName)
                .FirstOrDefaultAsync();
        }
    }
}
 