using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class UsageRecord
    {
        [Key]
        public Guid UsageRecordId { get; set; }

        public DateTime UsageDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int QuantityUsed { get; set; }

        public string UsageReason { get; set; } // Procedure, Maintenance, Damaged, etc.

        public string? Notes { get; set; }

        // Foreign Keys
        [ForeignKey("Item")]
        public Guid ItemId { get; set; }
        public Item Item { get; set; }

        // Optional: Link to Patient/Procedure if used in procedure
        [ForeignKey("Patient")]
        public Guid? PatientId { get; set; }
        public Patient Patient { get; set; }

        [ForeignKey("Procedure")]
        public Guid? ProcedureId { get; set; }
        public Procedure Procedure { get; set; }

        [ForeignKey("CreatedBy")]
        public string CreatedById { get; set; }
        public IdentityUser CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
