using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class BoardApproval
    {
        [Key]
        public Guid BoardApprovalId { get; set; }

        [ForeignKey("MedicalRecord")]
        public Guid MedicalRecordId { get; set; }
        public MedicalRecord MedicalRecord { get; set; }

        public DateTime BoardApprovalDate { get; set; }
        public bool BoardApprovalDecision { get; set; }
        public string BoardApprovalLetterNumber { get; set; }
        public string BoardApprovalAttachmentPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
