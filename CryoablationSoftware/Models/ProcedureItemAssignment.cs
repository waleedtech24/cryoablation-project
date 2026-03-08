using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class ProcedureItemAssignment
    {
        [Key]
        public Guid AssignmentId { get; set; }

        [ForeignKey("Procedure")]
        public Guid ProcedureId { get; set; }
        public Procedure Procedure { get; set; }

        [ForeignKey("Item")]
        public Guid ItemId { get; set; }
        public Item Item { get; set; }

        // Assignment Details
        public int QuantityAssigned { get; set; } // How many items allocated
        public string AssignmentReason { get; set; } // Why needed
        public string? Notes { get; set; }

        [ForeignKey("AssignedBy")]
        public string AssignedById { get; set; }
        public IdentityUser AssignedBy { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
