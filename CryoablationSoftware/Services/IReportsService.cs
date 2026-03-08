using CryoablationSoftware.ViewModel;

namespace CryoablationSoftware.Services
{
    public interface IReportsService
    {
        // Dashboard & Analytics
        Task<ReportsDashboardViewModel> GetReportsDashboardAsync();
        Task<AnalyticsDataViewModel> GetAnalyticsDataAsync();
        Task<object> GetChartDataAsync(string chartType, string period);
        Task<object> GetWidgetDataAsync(string widgetType);

        // Patient Reports
        Task<PatientReportsViewModel> GetPatientReportsAsync();
        Task<PatientDetailReportViewModel> GetPatientDetailReportAsync(Guid patientId, DateTime? fromDate, DateTime? toDate);

        // Procedure Reports
        Task<ProcedureReportViewModel> GetProcedureReportsAsync(DateTime? fromDate, DateTime? toDate, string status);
        Task<ProcedureAnalyticsViewModel> GetProcedureAnalyticsAsync();

        // Inventory Reports
        Task<InventoryReportViewModel> GetInventoryReportsAsync();
        Task<StockMovementReportViewModel> GetStockMovementReportAsync(DateTime? fromDate, DateTime? toDate, Guid? itemId);

        // Financial Reports
        Task<FinancialReportViewModel> GetFinancialReportsAsync(DateTime? fromDate, DateTime? toDate);

        // Custom Reports
        Task<CustomReportResultViewModel> GenerateCustomReportAsync(CustomReportViewModel model);
    }
}
