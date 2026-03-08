using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class ClinicalReport
    {
        [Key]
        public Guid ClinicalReportId { get; set; }

        public string ReportType { get; set; }        // e.g. CT Scan, X-Ray, Lab Report
        public string? ReportDescription { get; set; } // optional text
        public string ReportFilePath { get; set; }    // PDF/Image path
        public DateTime ReportDate { get; set; }

        [ForeignKey("PatientId")]
        public Guid? PatientId { get; set; }
        public Patient Patient { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
