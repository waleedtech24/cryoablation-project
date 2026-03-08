using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class ProcedureClinicalReport
    {

        [Key]
        public Guid ProcedureClinicalReportId { get; set; }

        [ForeignKey("Procedure")]
        public Guid ProcedureId { get; set; }
        public Procedure Procedure { get; set; }

        [ForeignKey("ClinicalReport")]
        public Guid ClinicalReportId { get; set; }
        public ClinicalReport ClinicalReport { get; set; }

        [ForeignKey("LinkedBy")]
        public string LinkedById { get; set; }
        public IdentityUser LinkedBy { get; set; }

        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
