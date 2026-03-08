using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class ProcedureDetailsViewModel
    {
        public Guid ProcedureId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientMRNumber { get; set; }

        public DateTime ProcedureDate { get; set; }
        public string ProcedureROI { get; set; }
        public string ProcedureDuration { get; set; }
        public string? ProcedureEquipmentUsed { get; set; }
        public string ProcedureRemarks { get; set; }
        public string ProcedureStatus { get; set; }
        public DateTime? ProcedureNextDate { get; set; }

        // NEW FIELDS - Doctor & Engineer Names
     
        public string PrimaryDoctorName { get; set; }

    
        public string? ConsultantDoctor1Name { get; set; }

 
        public string? ConsultantDoctor2Name { get; set; }

        public string OperatingEngineerName { get; set; }

        public string CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }

        // Item Details
        public List<ProcedureItemSummaryViewModel> ItemSummaries { get; set; } = new List<ProcedureItemSummaryViewModel>();

        // Clinical Reports
        public List<ProcedureClinicalReportViewModel> ClinicalReports { get; set; } = new List<ProcedureClinicalReportViewModel>();

        // Previous/Next Procedures
        public ProcedureBasicInfoViewModel? PreviousProcedure { get; set; }
        public ProcedureBasicInfoViewModel? NextProcedure { get; set; }


        // Medical Record Properties
        public bool HasMedicalRecord => !string.IsNullOrEmpty(MedicalRecordCancerStage) ||
                                        MedicalRecordCancerType?.Any() == true ||
                                        !string.IsNullOrEmpty(MedicalRecordComorbidities) ||
                                        !string.IsNullOrEmpty(MedicalRecordTumorLocation) ||
                                        !string.IsNullOrEmpty(MedicalRecordAllergies) ||
                                        !string.IsNullOrEmpty(MedicalRecordClinicalHistory);

        public List<string> MedicalRecordCancerType { get; set; } = new List<string>();
        public string? MedicalRecordCancerStage { get; set; }
        public string? MedicalRecordComorbidities { get; set; }
        public string? MedicalRecordTumorLocation { get; set; }
        public string? MedicalRecordAllergies { get; set; }
        public string? MedicalRecordClinicalHistory { get; set; }

        // Board Approval Properties
        public bool HasBoardApproval => BoardApprovalDate.HasValue ||
                                       BoardApprovalDecision.HasValue ||
                                       !string.IsNullOrEmpty(BoardApprovalLetterNumber) ||
                                       !string.IsNullOrEmpty(BoardApprovalAttachmentPath);

        public DateTime? BoardApprovalDate { get; set; }
        public bool? BoardApprovalDecision { get; set; }
        public string? BoardApprovalLetterNumber { get; set; }
        public string? BoardApprovalAttachmentPath { get; set; }
    }
}
