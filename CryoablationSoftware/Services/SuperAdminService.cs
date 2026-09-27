using CryoablationSoftware.Data;
using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace CryoablationSoftware.Services
{
    public interface ISuperAdminService
    {
        Task<SuperAdminDashboardViewModel> GetDashboardAsync();
        Task<SiteDetailViewModel> GetSiteDetailAsync(int siteId);
        Task<ComparativeAnalyticsViewModel> GetComparativeAnalyticsAsync();
        Task<PerformanceMetricsViewModel> GetPerformanceMetricsAsync();
        Task<ConsolidatedReportsViewModel> GetConsolidatedReportsAsync();
    }

    public class SuperAdminService : ISuperAdminService
    {
        private readonly ApplicationDbContext _context;

        public SuperAdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── MAIN DASHBOARD ───────────────────────────────────
        public async Task<SuperAdminDashboardViewModel> GetDashboardAsync()
        {
            var model = new SuperAdminDashboardViewModel();
            var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var sites = await _context.Sites.Where(s => s.IsActive).ToListAsync();
            model.TotalSites = sites.Count;

            // ── Sequential queries (EF Core DbContext is not thread-safe) ─
            model.TotalPatients = await _context.Patients.CountAsync(p => p.IsActive);
            model.TotalProcedures = await _context.Procedures.CountAsync(p => p.IsActive);
            model.TotalItems = await _context.Items.CountAsync(i => i.IsActive);
            model.PatientsThisMonth = await _context.Patients.CountAsync(p => p.IsActive && p.CreatedAt >= thisMonth);
            model.ProceduresThisMonth = await _context.Procedures.CountAsync(p => p.IsActive && p.ProcedureDate >= thisMonth);

            // ── Bulk grouped counts — ONE query each ──────────
            var patientsBySite = await _context.Patients
                .Where(p => p.IsActive && p.SiteId != null)
                .GroupBy(p => p.SiteId)
                .Select(g => new { SiteId = g.Key, Count = g.Count() })
                .ToListAsync();

            var patientsBySiteThisMonth = await _context.Patients
                .Where(p => p.IsActive && p.SiteId != null && p.CreatedAt >= thisMonth)
                .GroupBy(p => p.SiteId)
                .Select(g => new { SiteId = g.Key, Count = g.Count() })
                .ToListAsync();

            var itemsBySite = await _context.Items
                .Where(i => i.IsActive && i.SiteId != null)
                .GroupBy(i => i.SiteId)
                .Select(g => new { SiteId = g.Key, Count = g.Count() })
                .ToListAsync();

            var proceduresBySiteStatus = await _context.Procedures
                .Where(p => p.IsActive && p.SiteId != null)
                .GroupBy(p => new { p.SiteId, p.ProcedureStatus })
                .Select(g => new { g.Key.SiteId, g.Key.ProcedureStatus, Count = g.Count() })
                .ToListAsync();

            var proceduresBySiteThisMonth = await _context.Procedures
                .Where(p => p.IsActive && p.SiteId != null && p.ProcedureDate >= thisMonth)
                .GroupBy(p => p.SiteId)
                .Select(g => new { SiteId = g.Key, Count = g.Count() })
                .ToListAsync();

            // ── Bulk stock calculation — ONE query each ───────
            var stockReceived = await _context.StockRecords
                .Where(sr => sr.IsActive && sr.Item.SiteId != null)
                .GroupBy(sr => new { sr.Item.SiteId, sr.Item.ItemType, sr.Item.ItemName })
                .Select(g => new { g.Key.SiteId, g.Key.ItemType, g.Key.ItemName, Total = g.Sum(x => x.QuantityReceived) })
                .ToListAsync();

            var stockUsedRecords = await _context.UsageRecords
                .Where(ur => ur.IsActive && ur.Item.SiteId != null)
                .GroupBy(ur => new { ur.Item.SiteId, ur.Item.ItemType, ur.Item.ItemName })
                .Select(g => new { g.Key.SiteId, g.Key.ItemType, g.Key.ItemName, Total = g.Sum(x => x.QuantityUsed) })
                .ToListAsync();

            var stockUsedProcedures = await _context.ProcedureItemUsages
                .Where(piu => piu.IsActive && piu.Item.SiteId != null)
                .GroupBy(piu => new { piu.Item.SiteId, piu.Item.ItemType, piu.Item.ItemName })
                .Select(g => new { g.Key.SiteId, g.Key.ItemType, g.Key.ItemName, Total = g.Sum(x => x.QuantityUsed) })
                .ToListAsync();

            // ── Low stock — bulk ──────────────────────────────
            var allItemsForStock = await _context.Items
                .Where(i => i.IsActive)
                .Include(i => i.Site)
                .ToListAsync();

            var allStockIn = await _context.StockRecords
                .Where(sr => sr.IsActive)
                .GroupBy(sr => sr.ItemId)
                .Select(g => new { ItemId = g.Key, Total = g.Sum(x => x.QuantityReceived) })
                .ToListAsync();

            var allUsedRecords = await _context.UsageRecords
                .Where(ur => ur.IsActive)
                .GroupBy(ur => ur.ItemId)
                .Select(g => new { ItemId = g.Key, Total = g.Sum(x => x.QuantityUsed) })
                .ToListAsync();

            var allUsedProcedures = await _context.ProcedureItemUsages
                .Where(piu => piu.IsActive)
                .GroupBy(piu => piu.ItemId)
                .Select(g => new { ItemId = g.Key, Total = g.Sum(x => x.QuantityUsed) })
                .ToListAsync();

            // Convert to dictionaries for O(1) lookup
            var stockInDict = allStockIn.ToDictionary(x => x.ItemId, x => x.Total);
            var usedRecDict = allUsedRecords.ToDictionary(x => x.ItemId, x => x.Total);
            var usedProcDict = allUsedProcedures.ToDictionary(x => x.ItemId, x => x.Total);

            int GetStock(Guid itemId)
            {
                var r = stockInDict.GetValueOrDefault(itemId, 0);
                var u = usedRecDict.GetValueOrDefault(itemId, 0);
                //var p = usedProcDict.GetValueOrDefault(itemId, 0);
                return r - u;
            }

            // ── Build per-site summaries (no DB calls) ────────
            foreach (var site in sites)
            {
                var sid = (int?)site.SiteId;

                var siteSummary = new SiteSummaryViewModel
                {
                    SiteId = site.SiteId,
                    SiteName = site.SiteName,
                    SiteCity = site.SiteCity ?? "",

                    TotalPatients = patientsBySite.FirstOrDefault(x => x.SiteId == sid)?.Count ?? 0,
                    TotalProcedures = proceduresBySiteStatus.Where(x => x.SiteId == sid).Sum(x => x.Count),
                    TotalItems = itemsBySite.FirstOrDefault(x => x.SiteId == sid)?.Count ?? 0,
                    PatientsThisMonth = patientsBySiteThisMonth.FirstOrDefault(x => x.SiteId == sid)?.Count ?? 0,
                    ProceduresThisMonth = proceduresBySiteThisMonth.FirstOrDefault(x => x.SiteId == sid)?.Count ?? 0,

                    ScheduledProcedures = proceduresBySiteStatus.FirstOrDefault(x => x.SiteId == sid && x.ProcedureStatus == "Scheduled")?.Count ?? 0,
                    InProgressProcedures = proceduresBySiteStatus.FirstOrDefault(x => x.SiteId == sid && x.ProcedureStatus == "In-Progress")?.Count ?? 0,
                    CompletedProcedures = proceduresBySiteStatus.FirstOrDefault(x => x.SiteId == sid && x.ProcedureStatus == "Completed")?.Count ?? 0,
                    CancelledProcedures = proceduresBySiteStatus.FirstOrDefault(x => x.SiteId == sid && x.ProcedureStatus == "Cancelled")?.Count ?? 0,
                };

                // Probes & Liquid Nitrogen from in-memory data
                foreach (var item in allItemsForStock.Where(i => i.SiteId == site.SiteId))
                {
                    var stock = GetStock(item.ItemId);
                    if (item.ItemType == "Probe")
                        siteSummary.TotalProbes += stock;
                    if (item.ItemName.ToLower().Contains("nitrogen"))
                        siteSummary.LiquidNitrogen += stock;
                    if (item.ReorderLevel > 0)
                    {
                        if (stock <= 0) siteSummary.OutOfStockCount++;
                        else if (stock <= item.ReorderLevel) siteSummary.LowStockCount++;
                    }
                }

                model.Sites.Add(siteSummary);
            }

            // ── Low stock alerts — from in-memory ─────────────
            foreach (var item in allItemsForStock.Where(i => i.ReorderLevel > 0))
            {
                var stock = GetStock(item.ItemId);
                if (stock <= item.ReorderLevel)
                {
                    model.LowStockAlerts.Add(new SiteLowStockAlertViewModel
                    {
                        SiteId = item.SiteId ?? 0,
                        SiteName = item.Site?.SiteName ?? "Unknown",
                        ItemName = item.ItemName,
                        ItemCode = item.ItemCode,
                        CurrentStock = stock,
                        ReorderLevel = item.ReorderLevel,
                        Unit = item.Unit
                    });
                }
            }

            model.TotalLowStockAlerts = model.LowStockAlerts.Count;
            model.LowStockAlerts = model.LowStockAlerts
                .OrderBy(a => a.CurrentStock).Take(10).ToList();

            // ── Recent Activity — TWO queries only ────────────
            var recentProcs = await _context.Procedures
                .Where(p => p.IsActive)
                .Include(p => p.Patient)
                .Include(p => p.Site)
                .OrderByDescending(p => p.ProcedureDate)
                .Take(5)
                .ToListAsync();

            foreach (var proc in recentProcs)
                model.RecentActivity.Add(new SiteRecentActivityViewModel
                {
                    SiteId = proc.SiteId ?? 0,
                    SiteName = proc.Site?.SiteName ?? "Unknown",
                    ActivityType = "Procedure",
                    Description = $"{proc.Patient?.PatientName} — {proc.ProcedureROI} ({proc.ProcedureStatus})",
                    ActivityDate = proc.ProcedureDate,
                    BadgeClass = proc.ProcedureStatus == "Completed" ? "success" : proc.ProcedureStatus == "Scheduled" ? "primary" : "warning"
                });

            var recentStock = await _context.StockRecords
                .Where(sr => sr.IsActive)
                .Include(sr => sr.Item).ThenInclude(i => i.Site)
                .OrderByDescending(sr => sr.CreatedAt)
                .Take(3)
                .ToListAsync();

            foreach (var sr in recentStock)
                model.RecentActivity.Add(new SiteRecentActivityViewModel
                {
                    SiteId = sr.Item?.SiteId ?? 0,
                    SiteName = sr.Item?.Site?.SiteName ?? "Unknown",
                    ActivityType = "Stock",
                    Description = $"{sr.QuantityReceived} {sr.Item?.Unit} {sr.Item?.ItemName} received",
                    ActivityDate = sr.CreatedAt,
                    BadgeClass = "info"
                });

            model.RecentActivity = model.RecentActivity
                .OrderByDescending(a => a.ActivityDate).Take(8).ToList();

            // ── Monthly trends — ONE query ─────────────────────
            var trendData = await _context.Procedures
                .Where(p => p.IsActive && p.SiteId != null && p.ProcedureDate >= thisMonth.AddMonths(-5))
                .GroupBy(p => new { p.SiteId, p.ProcedureDate.Year, p.ProcedureDate.Month })
                .Select(g => new { g.Key.SiteId, g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var siteNameDict = sites.ToDictionary(s => (int?)s.SiteId, s => s.SiteName);

            for (int m = 5; m >= 0; m--)
            {
                var monthStart = thisMonth.AddMonths(-m);
                var monthLabel = monthStart.ToString("MMM");

                foreach (var site in sites)
                {
                    var count = trendData
                        .FirstOrDefault(t => t.SiteId == (int?)site.SiteId && t.Year == monthStart.Year && t.Month == monthStart.Month)
                        ?.Count ?? 0;

                    model.MonthlyTrends.Add(new SiteMonthlyTrendViewModel
                    {
                        Month = monthLabel,
                        SiteName = site.SiteName,
                        ProcedureCount = count
                    });
                }
            }

            return model;
        }

        // ── SITE DETAIL ──────────────────────────────────────
        public async Task<SiteDetailViewModel> GetSiteDetailAsync(int siteId)
        {
            var site = await _context.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId && s.IsActive);
            if (site == null) return null;

            var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Sequential counts (EF Core DbContext is not thread-safe)
            var model = new SiteDetailViewModel
            {
                SiteId = site.SiteId,
                SiteName = site.SiteName,
                SiteCity = site.SiteCity ?? "",
                TotalPatients = await _context.Patients.CountAsync(p => p.IsActive && p.SiteId == siteId),
                TotalProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.SiteId == siteId),
                PatientsThisMonth = await _context.Patients.CountAsync(p => p.IsActive && p.SiteId == siteId && p.CreatedAt >= thisMonth),
                ProceduresThisMonth = await _context.Procedures.CountAsync(p => p.IsActive && p.SiteId == siteId && p.ProcedureDate >= thisMonth),
                ScheduledProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.SiteId == siteId && p.ProcedureStatus == "Scheduled"),
                InProgressProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.SiteId == siteId && p.ProcedureStatus == "In-Progress"),
                CompletedProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.SiteId == siteId && p.ProcedureStatus == "Completed"),
                CancelledProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.SiteId == siteId && p.ProcedureStatus == "Cancelled"),
            };

            // Bulk stock for this site
            var siteItems = await _context.Items
                .Where(i => i.IsActive && i.SiteId == siteId)
                .ToListAsync();

            var itemIds = siteItems.Select(i => i.ItemId).ToList();

            var stockIn = await _context.StockRecords
                .Where(sr => sr.IsActive && itemIds.Contains(sr.ItemId))
                .GroupBy(sr => sr.ItemId)
                .Select(g => new { ItemId = g.Key, Total = g.Sum(x => x.QuantityReceived) })
                .ToListAsync();

            var usedRec = await _context.UsageRecords
                .Where(ur => ur.IsActive && itemIds.Contains(ur.ItemId))
                .GroupBy(ur => ur.ItemId)
                .Select(g => new { ItemId = g.Key, Total = g.Sum(x => x.QuantityUsed) })
                .ToListAsync();

            var usedProc = await _context.ProcedureItemUsages
                .Where(piu => piu.IsActive && itemIds.Contains(piu.ItemId))
                .GroupBy(piu => piu.ItemId)
                .Select(g => new { ItemId = g.Key, Total = g.Sum(x => x.QuantityUsed) })
                .ToListAsync();

            var sInDict = stockIn.ToDictionary(x => x.ItemId, x => x.Total);
            var uRecDict = usedRec.ToDictionary(x => x.ItemId, x => x.Total);
            var uProDict = usedProc.ToDictionary(x => x.ItemId, x => x.Total);

            int GetStock(Guid id)
            {
                return sInDict.GetValueOrDefault(id, 0)
                     - uRecDict.GetValueOrDefault(id, 0);
                   
            }

            foreach (var item in siteItems)
            {
                var stock = GetStock(item.ItemId);
                var name = item.ItemName.ToLower();

                if (item.ItemType == "Probe") model.TotalProbes += stock;
                if (name.Contains("sterile") || name.Contains("cover")) model.SterileCover += stock;
                if (name.Contains("ethanol")) model.Ethanol += stock;
                if (name.Contains("nitrogen")) model.LiquidNitrogen += stock;

                if (item.ReorderLevel > 0 && stock <= item.ReorderLevel)
                    model.LowStockItems.Add(new LowStockItemViewModel
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        ItemCode = item.ItemCode,
                        CurrentStock = stock,
                        ReorderLevel = item.ReorderLevel,
                        Unit = item.Unit
                    });
            }

            // Upcoming procedures
            model.UpcomingProcedures = await _context.Procedures
                .Where(p => p.IsActive && p.SiteId == siteId && p.ProcedureDate >= DateTime.Today && p.ProcedureStatus == "Scheduled")
                .Include(p => p.Patient)
                .OrderBy(p => p.ProcedureDate)
                .Take(7)
                .Select(p => new UpcomingProcedureViewModel
                {
                    ProcedureId = p.ProcedureId,
                    PatientName = p.Patient.PatientName,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDate = p.ProcedureDate,
                    Status = p.ProcedureStatus
                })
                .ToListAsync();

            // Recent activity
            var recentProcs = await _context.Procedures
                .Where(p => p.IsActive && p.SiteId == siteId)
                .Include(p => p.Patient)
                .OrderByDescending(p => p.ProcedureDate)
                .Take(5)
                .ToListAsync();

            foreach (var proc in recentProcs)
                model.RecentActivity.Add(new SiteRecentActivityViewModel
                {
                    SiteName = site.SiteName,
                    ActivityType = "Procedure",
                    Description = $"{proc.Patient?.PatientName} — {proc.ProcedureROI} ({proc.ProcedureStatus})",
                    ActivityDate = proc.ProcedureDate,
                    BadgeClass = proc.ProcedureStatus == "Completed" ? "success" : proc.ProcedureStatus == "Scheduled" ? "primary" : "warning"
                });

            // Monthly trends — ONE query
            var trendRaw = await _context.Procedures
                .Where(p => p.IsActive && p.SiteId == siteId && p.ProcedureDate >= thisMonth.AddMonths(-5))
                .GroupBy(p => new { p.ProcedureDate.Year, p.ProcedureDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            for (int m = 5; m >= 0; m--)
            {
                var ms = thisMonth.AddMonths(-m);
                var count = trendRaw.FirstOrDefault(t => t.Year == ms.Year && t.Month == ms.Month)?.Count ?? 0;
                model.MonthlyTrends.Add(new SiteMonthlyTrendViewModel
                {
                    Month = ms.ToString("MMM"),
                    SiteName = site.SiteName,
                    ProcedureCount = count
                });
            }

            // ROI breakdown
            model.ROIBreakdown = await _context.Procedures
                .Where(p => p.IsActive && p.SiteId == siteId && !string.IsNullOrEmpty(p.ProcedureROI))
                .GroupBy(p => p.ProcedureROI)
                .Select(g => new ROIBreakdownViewModel { ROI = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .Take(5)
                .ToListAsync();

            return model;
        }

        // ── COMPARATIVE ANALYTICS ────────────────────────────
        public async Task<ComparativeAnalyticsViewModel> GetComparativeAnalyticsAsync()
        {
            var dashboard = await GetDashboardAsync();
            return new ComparativeAnalyticsViewModel
            {
                Sites = dashboard.Sites,
                TotalPatients = dashboard.TotalPatients,
                TotalProcedures = dashboard.TotalProcedures,
                MonthlyTrends = dashboard.MonthlyTrends
            };
        }

        // ── PERFORMANCE METRICS ──────────────────────────────
        public async Task<PerformanceMetricsViewModel> GetPerformanceMetricsAsync()
        {
            var dashboard = await GetDashboardAsync();
            var model = new PerformanceMetricsViewModel
            {
                TotalProcedures = dashboard.TotalProcedures,
                TotalPatients = dashboard.TotalPatients,
                MonthlyTrends = dashboard.MonthlyTrends
            };

            foreach (var s in dashboard.Sites)
            {
                model.Sites.Add(new SitePerformanceViewModel
                {
                    SiteId = s.SiteId,
                    SiteName = s.SiteName,
                    SiteCity = s.SiteCity,
                    TotalProcedures = s.TotalProcedures,
                    TotalPatients = s.TotalPatients,
                    CompletedProcedures = s.CompletedProcedures,
                    ProceduresThisMonth = s.ProceduresThisMonth,
                    PatientsThisMonth = s.PatientsThisMonth,
                    LowStockCount = s.LowStockCount,
                    OutOfStockCount = s.OutOfStockCount
                });
            }

            return model;
        }

        // ── CONSOLIDATED REPORTS ─────────────────────────────
        public async Task<ConsolidatedReportsViewModel> GetConsolidatedReportsAsync()
        {
            var dashboard = await GetDashboardAsync();
            return new ConsolidatedReportsViewModel
            {
                Sites = dashboard.Sites,
                TotalPatients = dashboard.TotalPatients,
                TotalProcedures = dashboard.TotalProcedures,
                TotalItems = dashboard.TotalItems,
                TotalLowStock = dashboard.TotalLowStockAlerts,
                PatientsThisMonth = dashboard.PatientsThisMonth,
                ProceduresThisMonth = dashboard.ProceduresThisMonth,
                LowStockAlerts = dashboard.LowStockAlerts
            };
        }
    }
}