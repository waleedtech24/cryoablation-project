// ViewModels/Procedure/ProcedureCreateViewModel.cs - UPDATED
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class ProcedureCreateViewModel
    {
        [Required(ErrorMessage = "Please select a patient")]
        public Guid PatientId { get; set; }
        public string? PatientName { get; set; } // For display

        [Required(ErrorMessage = "Procedure date is required")]
        public DateTime ProcedureDate { get; set; } = DateTime.Now.Date.AddDays(1).AddHours(9);

        [Required(ErrorMessage = "ROI (Region of Interest) is required")]
        [StringLength(200, ErrorMessage = "ROI cannot exceed 200 characters")]
        public string ProcedureROI { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [StringLength(50, ErrorMessage = "Duration cannot exceed 50 characters")]
        public string ProcedureDuration { get; set; }

        // FIXED: Made Equipment Optional
        [StringLength(500, ErrorMessage = "Equipment details cannot exceed 500 characters")]
        public string? ProcedureEquipmentUsed { get; set; }

        // FIXED: Made Remarks Optional
        public string? ProcedureRemarks { get; set; }

        public string ProcedureStatus { get; set; } = "Scheduled";

        public DateTime? ProcedureNextDate { get; set; }

        public Guid? PreviousProcedureId { get; set; }

        // NEW FIELDS - Doctor & Engineer Names
        [Required(ErrorMessage = "Primary Doctor name is required")]
        [StringLength(200, ErrorMessage = "Primary Doctor name cannot exceed 200 characters")]
        [Display(Name = "Primary Doctor")]
        public string PrimaryDoctorName { get; set; }

        [StringLength(200, ErrorMessage = "Consultant Doctor 1 name cannot exceed 200 characters")]
        [Display(Name = "Consultant Doctor 1 (Optional)")]
        public string? ConsultantDoctor1Name { get; set; }

        [StringLength(200, ErrorMessage = "Consultant Doctor 2 name cannot exceed 200 characters")]
        [Display(Name = "Consultant Doctor 2 (Optional)")]
        public string? ConsultantDoctor2Name { get; set; }

        [Required(ErrorMessage = "Operating Engineer name is required")]
        [StringLength(200, ErrorMessage = "Operating Engineer name cannot exceed 200 characters")]
        [Display(Name = "Operating Engineer")]
        public string OperatingEngineerName { get; set; }


        // Medical Record Fields
        public List<string> MedicalRecordCancerType { get; set; } = new List<string>();
        public string? MedicalRecordCancerStage { get; set; }
        public string? MedicalRecordComorbidities { get; set; }
        public string? MedicalRecordTumorLocation { get; set; }

        [Display(Name = "Tumor Size")]
        public string? MedicalRecordAllergies { get; set; }
        public string? MedicalRecordClinicalHistory { get; set; }


        // Item Assignments
        public List<ProcedureItemAssignmentViewModel> ItemAssignments { get; set; } = new List<ProcedureItemAssignmentViewModel>();

        public List<ClinicalReportCreateViewModel> ClinicalReports { get; set; } = new List<ClinicalReportCreateViewModel>();


        // Board Approval
        public DateTime? BoardApprovalDate { get; set; }
        public bool? BoardApprovalDecision { get; set; }
        public string? BoardApprovalLetterNumber { get; set; }
        public IFormFile? BoardApprovalAttachment { get; set; }



    }

    public class ClinicalReportCreateViewModel
    {
        public string ReportType { get; set; }
        public IFormFile? ReportFile { get; set; }
    }


}