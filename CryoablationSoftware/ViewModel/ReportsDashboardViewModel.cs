using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    

    #region Dashboard ViewModels

    public class ReportsDashboardViewModel
    {
        // Key Metrics
        public int TotalPatients { get; set; }
        public int TotalProcedures { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalInventoryValue { get; set; }

        // This Month Statistics
        public int ProceduresThisMonth { get; set; }
        public int PatientsThisMonth { get; set; }

        // Alerts
        public List<LowStockItemViewModel> LowStockAlerts { get; set; } = new List<LowStockItemViewModel>();

        // Recent Activity
        public List<RecentActivityViewModel> RecentProcedures { get; set; } = new List<RecentActivityViewModel>();

        // Top Used Items
        public List<TopUsedItemViewModel> TopUsedItems { get; set; } = new List<TopUsedItemViewModel>();
    }

    public class AnalyticsDataViewModel
    {
        public List<MonthlyTrendViewModel> MonthlyProcedureTrends { get; set; } = new List<MonthlyTrendViewModel>();
        public List<StatusDistributionViewModel> ProcedureStatusDistribution { get; set; } = new List<StatusDistributionViewModel>();
        public List<CategoryPerformanceViewModel> TopCategories { get; set; } = new List<CategoryPerformanceViewModel>();
        public List<AgeGroupViewModel> PatientAgeGroups { get; set; } = new List<AgeGroupViewModel>();
        public List<GenderDistributionViewModel> GenderDistribution { get; set; } = new List<GenderDistributionViewModel>();
    }

    public class RecentActivityViewModel
    {
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string FormattedDate => Date.ToString("dd/MM/yyyy HH:mm");
    }

    public class TopUsedItemViewModel
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string Unit { get; set; }
        public int TotalUsed { get; set; }
    }

    public class MonthlyTrendViewModel
    {
        public string Month { get; set; }
        public int Count { get; set; }
    }

    public class StatusDistributionViewModel
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class CategoryPerformanceViewModel
    {
        public string Category { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class AgeGroupViewModel
    {
        public string AgeGroup { get; set; }
        public int Count { get; set; }
    }

    public class GenderDistributionViewModel
    {
        public string Gender { get; set; }
        public int Count { get; set; }
    }

    #endregion

    #region Patient Reports ViewModels

    public class PatientReportsViewModel
    {
        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int PatientsThisMonth { get; set; }

        public List<PatientSummaryViewModel> Patients { get; set; } = new List<PatientSummaryViewModel>();
        public List<AgeStatisticViewModel> PatientAgeStatistics { get; set; } = new List<AgeStatisticViewModel>();
    }

    public class PatientSummaryViewModel
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientMRNumber { get; set; }
        public string PatientPhone { get; set; }
        public int PatientAge { get; set; }
        public string PatientGender { get; set; }
        public int TotalProcedures { get; set; }
        public DateTime? LastProcedureDate { get; set; }
        public DateTime RegistrationDate { get; set; }

        public string FormattedRegistrationDate => RegistrationDate.ToString("dd/MM/yyyy");
        public string FormattedLastProcedure => LastProcedureDate?.ToString("dd/MM/yyyy") ?? "No Procedures";
    }

    public class PatientDetailReportViewModel
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientMRNumber { get; set; }
        public string PatientPhone { get; set; }
        public int PatientAge { get; set; }
        public string PatientGender { get; set; }
        public DateTime RegistrationDate { get; set; }

        public List<PatientProcedureDetailViewModel> Procedures { get; set; } = new List<PatientProcedureDetailViewModel>();

        // Statistics
        public int TotalProcedures { get; set; }
        public int CompletedProcedures { get; set; }
        public int ScheduledProcedures => Procedures.Count(p => p.ProcedureStatus == "Scheduled");
        public int InProgressProcedures => Procedures.Count(p => p.ProcedureStatus == "In-Progress");

        public string FormattedRegistrationDate => RegistrationDate.ToString("dd/MM/yyyy");
    }

    public class PatientProcedureDetailViewModel
    {
        public Guid ProcedureId { get; set; }
        public DateTime ProcedureDate { get; set; }
        public string ProcedureROI { get; set; }
        public string ProcedureDuration { get; set; }
        public string ProcedureStatus { get; set; }
        public string ProcedureRemarks { get; set; }
        public int TotalItemsUsed { get; set; }
        public string CreatedByName { get; set; }

        public string FormattedProcedureDate => ProcedureDate.ToString("dd/MM/yyyy HH:mm");
    }

    public class AgeStatisticViewModel
    {
        public string AgeGroup { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    #endregion

    #region Procedure Reports ViewModels

    public class ProcedureReportViewModel
    {
        public List<ProcedureReportDetailViewModel> Procedures { get; set; } = new List<ProcedureReportDetailViewModel>();

        // Statistics
        public int TotalProcedures { get; set; }
        public int CompletedProcedures { get; set; }
        public int ScheduledProcedures { get; set; }
        public int InProgressProcedures { get; set; }
        public int CancelledProcedures { get; set; }

        // Performance Metrics
        public double CompletionRate => TotalProcedures > 0 ? (double)CompletedProcedures / TotalProcedures * 100 : 0;
    }

    public class ProcedureReportDetailViewModel
    {
        public Guid ProcedureId { get; set; }
        public DateTime ProcedureDate { get; set; }
        public string PatientName { get; set; }
        public string PatientMRNumber { get; set; }
        public string PatientCNIC { get; set; }
        public string ProcedureROI { get; set; }
        public string ProcedureDuration { get; set; }
        public string ProcedureStatus { get; set; }
        public int TotalItemsAssigned { get; set; }
        public int TotalItemsUsed { get; set; }
        public string CreatedByName { get; set; }
        public string ProcedureRemarks { get; set; }

        public string PrimaryDoctorName { get; set; }


        public string ConsultantDoctor1Name { get; set; }

        
        public string ConsultantDoctor2Name { get; set; }

        public string FormattedProcedureDate => ProcedureDate.ToString("dd/MM/yyyy HH:mm");
        public string StatusBadgeClass => ProcedureStatus switch
        {
            "Completed" => "success",
            "Scheduled" => "primary",
            "In-Progress" => "warning",
            "Cancelled" => "danger",
            _ => "secondary"
        };
    }

    public class ProcedureAnalyticsViewModel
    {
        public List<ProcedureMonthlyTrendViewModel> MonthlyTrends { get; set; } = new List<ProcedureMonthlyTrendViewModel>();
        public List<ROIAnalysisViewModel> ROIAnalysis { get; set; } = new List<ROIAnalysisViewModel>();
        public List<ProcedureDurationAnalysisViewModel> DurationAnalysis { get; set; } = new List<ProcedureDurationAnalysisViewModel>();
    }

    public class ProcedureMonthlyTrendViewModel
    {
        public string Month { get; set; }
        public int Count { get; set; }
        public int CompletedCount { get; set; }
        public double CompletionRate => Count > 0 ? (double)CompletedCount / Count * 100 : 0;
    }

    public class ROIAnalysisViewModel
    {
        public string ROI { get; set; }
        public int Count { get; set; }
        public double CompletionRate { get; set; }
    }

    public class ProcedureDurationAnalysisViewModel
    {
        public string ROI { get; set; }
        public double AverageDuration { get; set; }
        public int ProcedureCount { get; set; }
    }

    #endregion

    #region Inventory Reports ViewModels

    public class InventoryReportViewModel
    {
        public int TotalItems { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public decimal TotalInventoryValue { get; set; }

        public List<ItemStockDetailViewModel> ItemStockDetails { get; set; } = new List<ItemStockDetailViewModel>();
        public List<CategoryAnalysisViewModel> CategoryAnalysis { get; set; } = new List<CategoryAnalysisViewModel>();
    }

    public class ItemStockDetailViewModel
    {
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public decimal TotalValue { get; set; }
        public string Status { get; set; }

        public string StatusBadgeClass => Status switch
        {
            "Out of Stock" => "danger",
            "Low Stock" => "warning",
            "Normal" => "success",
            _ => "secondary"
        };

        public bool IsCritical => Status == "Out of Stock" || Status == "Low Stock";
    }

    public class CategoryAnalysisViewModel
    {
        public string Category { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }

        public double LowStockPercentage => ItemCount > 0 ? (double)LowStockCount / ItemCount * 100 : 0;
    }

    public class StockMovementReportViewModel
    {
        public List<StockMovementDetailViewModel> StockMovements { get; set; } = new List<StockMovementDetailViewModel>();

        // Summary
        public int TotalInQuantity { get; set; }
        public int TotalOutQuantity { get; set; }
        public decimal TotalInValue { get; set; }
        public decimal TotalOutValue { get; set; }
        public int NetMovement { get; set; }
    }

    public class StockMovementDetailViewModel
    {
        public DateTime Date { get; set; }
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string MovementType { get; set; } // IN/OUT
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public string Reason { get; set; }
        public string UserName { get; set; }
        public string Notes { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }

        public string FormattedDate => Date.ToString("dd/MM/yyyy HH:mm");
        public string MovementBadgeClass => MovementType == "IN" ? "success" : "danger";
        public string MovementIcon => MovementType == "IN" ? "fas fa-arrow-down" : "fas fa-arrow-up";
    }

    #endregion

    #region Financial Reports ViewModels

    public class FinancialReportViewModel
    {
        public decimal TotalPurchases { get; set; }
        public int TotalQuantityPurchased { get; set; }
        public decimal TotalConsumptionValue { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal CurrentStockValue { get; set; }

        public List<MonthlyFinancialViewModel> MonthlyFinancialTrends { get; set; } = new List<MonthlyFinancialViewModel>();
        public List<CategorySpendingViewModel> CategoryWiseSpending { get; set; } = new List<CategorySpendingViewModel>();

        // Performance Metrics
        public decimal AveragePurchaseCost => TotalQuantityPurchased > 0 ? TotalPurchases / TotalQuantityPurchased : 0;
        public decimal InventoryTurnover => CurrentStockValue > 0 ? TotalConsumptionValue / CurrentStockValue : 0;
    }

    public class MonthlyFinancialViewModel
    {
        public string Month { get; set; }
        public decimal PurchaseAmount { get; set; }
        public int PurchaseQuantity { get; set; }

        public decimal AverageUnitCost => PurchaseQuantity > 0 ? PurchaseAmount / PurchaseQuantity : 0;
    }

    public class CategorySpendingViewModel
    {
        public string Category { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalQuantity { get; set; }
        public int ItemCount { get; set; }

        public decimal AverageItemCost => ItemCount > 0 ? TotalSpent / ItemCount : 0;
    }

    #endregion

    #region Custom Reports ViewModels

    public class CustomReportViewModel
    {
        [Required(ErrorMessage = "Report title is required")]
        public string ReportTitle { get; set; }

        [Required(ErrorMessage = "Report type is required")]
        public string ReportType { get; set; } // patient_procedures, inventory_usage, financial_summary

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<Guid> SelectedPatients { get; set; } = new List<Guid>();
        public List<Guid> SelectedItems { get; set; } = new List<Guid>();
        public List<string> SelectedCategories { get; set; } = new List<string>();
        public List<string> SelectedStatuses { get; set; } = new List<string>();

        public string ExportFormat { get; set; } = "View"; // View, Excel, PDF

        public string GroupBy { get; set; } // Month, Category, Status, etc.
        public bool IncludeSummary { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;

        // Additional filters
        public bool FilterByDateRange => FromDate.HasValue && ToDate.HasValue;
        public bool FilterByPatients => SelectedPatients.Any();
        public bool FilterByItems => SelectedItems.Any();
    }

    public class CustomReportResultViewModel
    {
        public string ReportTitle { get; set; }
        public DateTime GeneratedDate { get; set; }
        public CustomReportViewModel Parameters { get; set; }
        public object Data { get; set; }

        // Metadata
        public int TotalRecords { get; set; }
        public string GeneratedBy { get; set; }
        public TimeSpan GenerationTime { get; set; }
    }

    #endregion

    #region Common ViewModels

    public class ReportFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Status { get; set; }
        public Guid? PatientId { get; set; }
        public Guid? ItemId { get; set; }
        public string Category { get; set; }

        // For dropdowns
        public List<SelectListItem> Patients { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Items { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
    }

    public class SelectListItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
    }

    public class ChartDataViewModel
    {
        public string Label { get; set; }
        public object Data { get; set; }
        public string ChartType { get; set; } // line, bar, pie, doughnut
        public List<string> Labels { get; set; } = new List<string>();
        public List<ChartDatasetViewModel> Datasets { get; set; } = new List<ChartDatasetViewModel>();
    }

    public class ChartDatasetViewModel
    {
        public string Label { get; set; }
        public List<decimal> Data { get; set; } = new List<decimal>();
        public string BackgroundColor { get; set; }
        public string BorderColor { get; set; }
        public int BorderWidth { get; set; } = 1;
    }

    public class WidgetViewModel
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string Change { get; set; }
        public string ChangeType { get; set; } // increase, decrease, neutral
        public object Data { get; set; }
    }

    public class ExportOptionsViewModel
    {
        public string Format { get; set; } // Excel, PDF, CSV
        public bool IncludeCharts { get; set; }
        public bool IncludeSummary { get; set; }
        public string PaperSize { get; set; } = "A4";
        public string Orientation { get; set; } = "Portrait";
        public bool IncludeHeaders { get; set; } = true;
        public bool IncludeFooters { get; set; } = true;
    }

    #endregion
}
