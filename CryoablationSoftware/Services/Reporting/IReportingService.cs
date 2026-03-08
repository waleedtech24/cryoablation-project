
using CryoablationSoftware.ViewModel.Reporting;
using CryoablationSoftware.ViewModel;

namespace CryoablationSoftware.Services.Reporting
{
    public interface IReportingService
    {
        // Main Reporting Methods
        Task<ReportingIndexViewModel> GetReportingIndexAsync();
        Task<List<ReportTemplateViewModel>> GetReportTemplatesAsync();
        Task<GeneratedReportViewModel> GenerateReportAsync(ReportGenerationViewModel model, string userId);

        // Specific Report Generators
        Task<PatientProcedureReportViewModel> GeneratePatientProcedureReportAsync(Guid patientId, DateTime? fromDate, DateTime? toDate);
        Task<InventoryConsumptionReportViewModel> GenerateInventoryConsumptionReportAsync(DateTime fromDate, DateTime toDate, List<string> categories = null);
        Task<FinancialSummaryReportViewModel> GenerateFinancialSummaryReportAsync(DateTime fromDate, DateTime toDate);

        // Export Methods
        Task<byte[]> ExportReportToPdfAsync(GeneratedReportViewModel report);
        Task<byte[]> ExportReportToExcelAsync(GeneratedReportViewModel report);
        Task<byte[]> ExportReportToWordAsync(GeneratedReportViewModel report);

        // Utility Methods
        Task<List<PatientSummaryViewModel>> GetPatientsForReportAsync();
        Task<List<ItemSummaryViewModel>> GetItemsForReportAsync();
        Task<List<string>> GetCategoriesForReportAsync();
        Task<ReportValidationResult> ValidateReportParametersAsync(ReportGenerationViewModel model);
    }
}
