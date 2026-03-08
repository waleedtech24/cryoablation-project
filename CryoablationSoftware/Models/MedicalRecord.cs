using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class MedicalRecord
    {
        [Key]
        public Guid MedicalRecordId { get; set; }

        [ForeignKey("Patient")]
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; }

        public string MedicalRecordCancerType { get; set; } // e.g. comma-separated string or JSON
        public string MedicalRecordCancerStage { get; set; }
        public string MedicalRecordComorbidities { get; set; }
        public string? MedicalRecordTumorLocation { get; set; }

        [Display(Name = "Tumor Size")]
        public string MedicalRecordAllergies { get; set; }
        public string MedicalRecordClinicalHistory { get; set; }

        //public string MedicalRecordImagingReportPath { get; set; }
        //public string MedicalRecordLabReportPath { get; set; }

        public ICollection<ClinicalReport> ClinicalReports { get; set; }
      
    //    public BoardApproval BoardApproval { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}