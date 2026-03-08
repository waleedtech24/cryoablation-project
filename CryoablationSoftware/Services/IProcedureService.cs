using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModel;

namespace CryoablationSoftware.Services
{
    public interface IProcedureService
    {
        // Procedure Management
        Task<IEnumerable<ProcedureListViewModel>> GetAllProceduresAsync();
        Task<ProcedureDetailsViewModel> GetProcedureDetailsAsync(Guid procedureId);
        Task<Procedure> GetProcedureByIdAsync(Guid procedureId);
        Task<Procedure> GetProcedureForEditAsync(Guid procedureId);

        Task<bool> CreateProcedureAsync(ProcedureCreateViewModel model, string userId);
        Task<bool> UpdateProcedureAsync(ProcedureEditViewModel model);
        Task<bool> UpdateProcedureAsync(ProcedureEditViewModel model, string userId);
        Task<bool> UpdateProcedureStatusAsync(Guid procedureId, string newStatus);
        Task<bool> DeleteProcedureAsync(Guid procedureId);

        // Item Assignment & Usage
        Task<bool> AssignItemToProcedureAsync(Guid procedureId, Guid itemId, int quantity, string reason, string? notes, string userId);
        Task<bool> RecordItemUsageAsync(ProcedureItemUsageViewModel model, string userId);
        Task<bool> RemoveItemAssignmentAsync(Guid assignmentId);
        Task<List<ProcedureItemSummaryViewModel>> GetProcedureItemSummaryAsync(Guid procedureId);
        Task<AssignmentResultViewModel> AssignMultipleItemsToProcedureAsync(Guid procedureId, List<AssignItemViewModel> items, string? overallNotes, string userId);
        Task<IEnumerable<ProcedureItemAssignment>> GetAssignedItemsAsync(Guid procedureId);
        Task<AssignmentResultViewModel> UpdateAssignedItemsAsync(Guid procedureId, List<EditAssignItemViewModel> items, string? overallNotes, string userId);

        // Clinical Reports
        Task<bool> LinkClinicalReportAsync(Guid procedureId, Guid clinicalReportId, string userId);
        Task<bool> UnlinkClinicalReportAsync(Guid procedureId, Guid clinicalReportId);
        Task<List<AvailableClinicalReportViewModel>> GetAvailableClinicalReportsAsync(Guid patientId, Guid procedureId);

        // Dashboard & Analytics
        Task<ProcedureDashboardViewModel> GetDashboardDataAsync();
        Task<IEnumerable<ProcedureListViewModel>> GetProceduresByPatientAsync(Guid patientId);
        Task<IEnumerable<ProcedureListViewModel>> GetProceduresByStatusAsync(string status);
        Task<IEnumerable<ProcedureListViewModel>> GetProceduresByStatusesAsync(string status);


        // Utility Methods
        Task<IEnumerable<Patient>> GetActivePatientsAsync();
        Task<IEnumerable<Item>> GetAvailableItemsAsync();
        Task<int> GetAvailableStockAsync(Guid itemId);
        Task<bool> ValidateItemUsageAsync(Guid procedureId, Guid itemId, int quantityToUse);

        // Add these methods to IProcedureService.cs interface
        Task<bool> ChangeProcedureStatusAsync(Guid procedureId, string newStatus);
        Task<bool> CompleteProcedureAsync(Guid procedureId, string irRemarks, DateTime? dischargeDate, DateTime? nextProcedureDate, string? completionNotes);


    }
}
