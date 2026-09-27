namespace CryoablationSoftware.ViewModel
{
    // ── MAIN DASHBOARD ──────────────────────────────────────
    public class SuperAdminDashboardViewModel
    {
        public int TotalSites { get; set; }
        public int TotalPatients { get; set; }
        public int TotalProcedures { get; set; }
        public int TotalItems { get; set; }
        public int TotalLowStockAlerts { get; set; }
        public int PatientsThisMonth { get; set; }
        public int ProceduresThisMonth { get; set; }
        public List<SiteSummaryViewModel> Sites { get; set; } = new();
        public List<SiteLowStockAlertViewModel> LowStockAlerts { get; set; } = new();
        public List<SiteRecentActivityViewModel> RecentActivity { get; set; } = new();
        public List<SiteMonthlyTrendViewModel> MonthlyTrends { get; set; } = new();
    }

    // ── SITE SUMMARY (used in Dashboard + Comparative + Reports) ─
    public class SiteSummaryViewModel
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public string SiteCity { get; set; }
        public int TotalPatients { get; set; }
        public int TotalProcedures { get; set; }
        public int TotalItems { get; set; }
        public int PatientsThisMonth { get; set; }
        public int ProceduresThisMonth { get; set; }
        public int ScheduledProcedures { get; set; }
        public int InProgressProcedures { get; set; }
        public int CompletedProcedures { get; set; }
        public int CancelledProcedures { get; set; }
        public int TotalProbes { get; set; }
        public int LiquidNitrogen { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public string InventoryStatus => OutOfStockCount > 0 ? "Out of Stock" : LowStockCount > 0 ? "Low Stock" : "Healthy";
        public string InventoryStatusClass => OutOfStockCount > 0 ? "danger" : LowStockCount > 0 ? "warning" : "success";
    }

    // ── LOW STOCK ALERT ─────────────────────────────────────
    public class SiteLowStockAlertViewModel
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public string Unit { get; set; }
        public string Status => CurrentStock <= 0 ? "Out of Stock" : "Low Stock";
        public string StatusClass => CurrentStock <= 0 ? "danger" : "warning";
    }

    // ── RECENT ACTIVITY ─────────────────────────────────────
    public class SiteRecentActivityViewModel
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public DateTime ActivityDate { get; set; }
        public string BadgeClass { get; set; }
    }

    // ── MONTHLY TREND ────────────────────────────────────────
    public class SiteMonthlyTrendViewModel
    {
        public string Month { get; set; }
        public string SiteName { get; set; }
        public int ProcedureCount { get; set; }
    }

    // ── SITE DETAIL ─────────────────────────────────────────
    public class SiteDetailViewModel
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public string SiteCity { get; set; }
        public int TotalPatients { get; set; }
        public int TotalProcedures { get; set; }
        public int PatientsThisMonth { get; set; }
        public int ProceduresThisMonth { get; set; }
        public int ScheduledProcedures { get; set; }
        public int InProgressProcedures { get; set; }
        public int CompletedProcedures { get; set; }
        public int CancelledProcedures { get; set; }
        public int TotalProbes { get; set; }
        public int SterileCover { get; set; }
        public int Ethanol { get; set; }
        public int LiquidNitrogen { get; set; }
        public List<LowStockItemViewModel> LowStockItems { get; set; } = new();
        public List<SiteRecentActivityViewModel> RecentActivity { get; set; } = new();
        public List<UpcomingProcedureViewModel> UpcomingProcedures { get; set; } = new();
        public List<SiteMonthlyTrendViewModel> MonthlyTrends { get; set; } = new();
        public List<ROIBreakdownViewModel> ROIBreakdown { get; set; } = new();
    }

    // ── ROI BREAKDOWN ────────────────────────────────────────
    public class ROIBreakdownViewModel
    {
        public string ROI { get; set; }
        public int Count { get; set; }
    }

    // ── COMPARATIVE ANALYTICS ───────────────────────────────
    public class ComparativeAnalyticsViewModel
    {
        public List<SiteSummaryViewModel> Sites { get; set; } = new();
        public int TotalPatients { get; set; }
        public int TotalProcedures { get; set; }
        public List<SiteMonthlyTrendViewModel> MonthlyTrends { get; set; } = new();
    }

    // ── PERFORMANCE METRICS ─────────────────────────────────
    public class PerformanceMetricsViewModel
    {
        public List<SitePerformanceViewModel> Sites { get; set; } = new();
        public int TotalProcedures { get; set; }
        public int TotalPatients { get; set; }
        public List<SiteMonthlyTrendViewModel> MonthlyTrends { get; set; } = new();
    }

    public class SitePerformanceViewModel
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public string SiteCity { get; set; }
        public int TotalProcedures { get; set; }
        public int TotalPatients { get; set; }
        public int CompletedProcedures { get; set; }
        public int ProceduresThisMonth { get; set; }
        public int PatientsThisMonth { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public string InventoryStatus => OutOfStockCount > 0 ? "Out of Stock" : LowStockCount > 0 ? "Low Stock" : "Healthy";
        public string InventoryStatusClass => OutOfStockCount > 0 ? "danger" : LowStockCount > 0 ? "warning" : "success";
        public double CompletionRate => TotalProcedures > 0
            ? Math.Round((double)CompletedProcedures / TotalProcedures * 100, 1) : 0;
    }

    // ── CONSOLIDATED REPORTS ────────────────────────────────
    public class ConsolidatedReportsViewModel
    {
        public List<SiteSummaryViewModel> Sites { get; set; } = new();
        public int TotalPatients { get; set; }
        public int TotalProcedures { get; set; }
        public int TotalItems { get; set; }
        public int TotalLowStock { get; set; }
        public int PatientsThisMonth { get; set; }
        public int ProceduresThisMonth { get; set; }
        public List<SiteLowStockAlertViewModel> LowStockAlerts { get; set; } = new();
    }
}