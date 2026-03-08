using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class ProcedureItemUsage
    {
        [Key]
        public Guid UsageId { get; set; }

        [ForeignKey("Procedure")]
        public Guid ProcedureId { get; set; }
        public Procedure Procedure { get; set; }

        [ForeignKey("Item")]
        public Guid ItemId { get; set; }
        public Item Item { get; set; }

        // Usage Details
        public int QuantityUsed { get; set; } // Actually consumed
        public DateTime UsageDateTime { get; set; }
        public string? UsageNotes { get; set; }

        [ForeignKey("RecordedBy")]
        public string RecordedById { get; set; }
        public IdentityUser RecordedBy { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
