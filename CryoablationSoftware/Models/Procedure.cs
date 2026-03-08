using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CryoablationSoftware.Models
{
    public class Procedure
    {
        [Key]
        public Guid ProcedureId { get; set; }

        [ForeignKey("Patient")]
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; }

        [ForeignKey("CreatedBy")]
        public string CreatedById { get; set; }
        public IdentityUser CreatedBy { get; set; }

        // Procedure Details
        public DateTime ProcedureDate { get; set; }
        public string ProcedureROI { get; set; } // Region of Interest
        public string ProcedureDuration { get; set; }
        public string? ProcedureEquipmentUsed { get; set; }
        public string? ProcedureRemarks { get; set; }

        // NEW FIELDS - Doctor & Engineer Names for this specific procedure
        [Required(ErrorMessage = "Primary Doctor name is required")]
        [StringLength(200)]
        [Display(Name = "Primary Doctor")]
        public string PrimaryDoctorName { get; set; }

        [StringLength(200)]
        [Display(Name = "Consultant Doctor 1")]
        public string? ConsultantDoctor1Name { get; set; }

        [StringLength(200)]
        [Display(Name = "Consultant Doctor 2")]
        public string? ConsultantDoctor2Name { get; set; }

        [Required(ErrorMessage = "Operating Engineer name is required")]
        [StringLength(200)]
        [Display(Name = "Operating Engineer")]
        public string OperatingEngineerName { get; set; }


        // Status & Scheduling
        public string ProcedureStatus { get; set; } = "Scheduled";
        // Values: Scheduled, In-Progress, Completed, Cancelled
        public DateTime? ProcedureNextDate { get; set; }
        public Guid? PreviousProcedureId { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public ICollection<ProcedureItemAssignment> ItemAssignments { get; set; }
        public ICollection<ProcedureItemUsage> ItemUsages { get; set; }
        public ICollection<ProcedureClinicalReport> ClinicalReports { get; set; }
    }
}
