using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel.Reporting
{
    public class AllReportingViewModel
    {
    }
    public class ReportingIndexViewModel
    {
        public int TotalReports { get; set; }
        public int ReportsThisMonth { get; set; }
        public List<ReportCategoryViewModel> ReportCategories { get; set; } = new();
        public List<RecentReportViewModel> RecentReports { get; set; } = new();
    }

    public class ReportCategoryViewModel
    {
        public string CategoryName { get; set; }
        public string CategoryIcon { get; set; }
        public int ReportCount { get; set; }
        public List<ReportTemplateViewModel> Templates { get; set; } = new();
    }

    public class ReportTemplateViewModel
    {
        public string TemplateName { get; set; }
        public string Description { get; set; }
        public string ReportType { get; set; }
        public bool RequiresDateRange { get; set; }
        public bool RequiresPatientSelection { get; set; }
        public bool RequiresItemSelection { get; set; }
    }

    public class RecentReportViewModel
    {
        public string ReportName { get; set; }
        public string ReportType { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; }
        public string Status { get; set; }
    }

    // Report Generation ViewModel
    public class ReportGenerationViewModel
    {
        [Required(ErrorMessage = "Report title is required")]
        public string ReportTitle { get; set; }

        [Required(ErrorMessage = "Report type is required")]
        public string ReportType { get; set; }

        // Date Range
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Patient Selection
        public List<Guid> SelectedPatients { get; set; } = new();
        public bool IncludeAllPatients { get; set; } = true;

        // Item/Category Selection
        public List<Guid> SelectedItems { get; set; } = new();
        public List<string> SelectedCategories { get; set; } = new();
        public bool IncludeAllItems { get; set; } = true;

        // Report Options
        public bool IncludeSummary { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeDetailedData { get; set; } = true;
        public bool IncludeRecommendations { get; set; } = false;

        // Output Options
        public string OutputFormat { get; set; } = "PDF"; // PDF, Excel, Word
        public string PageSize { get; set; } = "A4";
        public string Orientation { get; set; } = "Portrait";

        // Grouping and Sorting
        public string? GroupBy { get; set; } // Date, Category, Patient, etc.
        public string SortBy { get; set; } = "Date";
        public string SortOrder { get; set; } = "DESC";

        // Filters
        public string? StatusFilter { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
    }

    // Generated Report Result ViewModel
    public class GeneratedReportViewModel
    {
        public string ReportId { get; set; }
        public string ReportTitle { get; set; }
        public string ReportType { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; }
        public ReportGenerationViewModel Parameters { get; set; }

        // Report Data
        public object ReportData { get; set; }
        public ReportSummaryViewModel Summary { get; set; }
        public List<ReportSectionViewModel> Sections { get; set; } = new();

        // Metadata
        public int TotalRecords { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
    }

    public class ReportSummaryViewModel
    {
        public Dictionary<string, object> KeyMetrics { get; set; } = new();
        public List<string> KeyFindings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class ReportSectionViewModel
    {
        public string SectionTitle { get; set; }
        public string SectionType { get; set; } // Table, Chart, Text, Summary
        public object SectionData { get; set; }
        public string ChartType { get; set; } // For chart sections
        public List<ReportColumnViewModel> Columns { get; set; } = new();
    }

    public class ReportColumnViewModel
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string Format { get; set; }
        public bool IsVisible { get; set; } = true;
        public int Width { get; set; }
    }

    // Specific Report ViewModels
    public class PatientProcedureReportViewModel
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientMRNumber { get; set; }
        public string PatientPhone { get; set; }
        public int PatientAge { get; set; }
        public string PatientGender { get; set; }
        public DateTime RegistrationDate { get; set; }

        public List<ProcedureReportDetailViewModel> Procedures { get; set; } = new();
        public PatientReportSummaryViewModel Summary { get; set; }
    }

    public class PatientReportSummaryViewModel
    {
        public int TotalProcedures { get; set; }
        public int CompletedProcedures { get; set; }
        public int ScheduledProcedures { get; set; }
        public int CancelledProcedures { get; set; }
        public decimal TotalItemsUsed { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime LastProcedureDate { get; set; }
        public string MostCommonROI { get; set; }
    }

    public class InventoryConsumptionReportViewModel
    {
        public List<ItemConsumptionDetailViewModel> Items { get; set; } = new();
        public InventoryConsumptionSummaryViewModel Summary { get; set; }
        public List<CategoryConsumptionViewModel> CategoryBreakdown { get; set; } = new();
    }

    public class ItemConsumptionDetailViewModel
    {
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public int OpeningStock { get; set; }
        public int StockReceived { get; set; }
        public int StockUsed { get; set; }
        public int ClosingStock { get; set; }
        public decimal AverageUnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public int ProcedureCount { get; set; }
        public double ConsumptionRate { get; set; }
    }

    public class InventoryConsumptionSummaryViewModel
    {
        public decimal TotalOpeningValue { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalConsumptionValue { get; set; }
        public decimal TotalClosingValue { get; set; }
        public int TotalItemsConsumed { get; set; }
        public int TotalProcedures { get; set; }
        public decimal AverageConsumptionPerProcedure { get; set; }
        public string HighestConsumptionCategory { get; set; }
        public string HighestConsumptionItem { get; set; }
    }

    public class CategoryConsumptionViewModel
    {
        public string Category { get; set; }
        public int ItemCount { get; set; }
        public int TotalConsumed { get; set; }
        public decimal TotalValue { get; set; }
        public double PercentageOfTotal { get; set; }
    }

    public class FinancialSummaryReportViewModel
    {
        public DateTime ReportPeriodFrom { get; set; }
        public DateTime ReportPeriodTo { get; set; }

        public FinancialMetricsViewModel Metrics { get; set; }
        public List<MonthlyFinancialDataViewModel> MonthlyData { get; set; } = new();
        public List<CategoryFinancialDataViewModel> CategoryData { get; set; } = new();
        public FinancialAnalysisViewModel Analysis { get; set; }
    }

    public class FinancialMetricsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal InventoryPurchases { get; set; }
        public decimal InventoryConsumption { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ROI { get; set; }
        public int TotalProcedures { get; set; }
        public decimal AverageRevenuePerProcedure { get; set; }
        public decimal AverageCostPerProcedure { get; set; }
    }

    public class MonthlyFinancialDataViewModel
    {
        public string Month { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal Purchases { get; set; }
        public decimal Consumption { get; set; }
        public int ProcedureCount { get; set; }
        public decimal Profit { get; set; }
    }

    public class CategoryFinancialDataViewModel
    {
        public string Category { get; set; }
        public decimal PurchaseAmount { get; set; }
        public decimal ConsumptionAmount { get; set; }
        public int ItemCount { get; set; }
        public double ConsumptionPercentage { get; set; }
    }

    public class FinancialAnalysisViewModel
    {
        public List<string> KeyInsights { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public TrendAnalysisViewModel Trends { get; set; }
    }

    public class TrendAnalysisViewModel
    {
        public double RevenueGrowthRate { get; set; }
        public double ExpenseGrowthRate { get; set; }
        public double ProfitGrowthRate { get; set; }
        public string Trend { get; set; } // Positive, Negative, Stable
    }
    public class ReportValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }



    public class ItemSummaryViewModel
    {
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public int CurrentStock { get; set; }
    }

    public class SelectListItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
    }
}
