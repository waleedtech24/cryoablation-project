using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryoablationSoftware.Models
{
    public class Item
    {
        [Key]
        public Guid ItemId { get; set; }

        [Required]
        [StringLength(100)]
        public string ItemName { get; set; }

        [Required]
        [StringLength(500)]
        public string ItemCode { get; set; } // Unique identifier like SYR001

        [Required]
        [StringLength(50)]
        public string Category { get; set; } // Consumables, Equipment, Medicines, etc.

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } // Pieces, Box, Liters, KG

        [Required]
        public string ItemType { get; set; } = "Probe"; // Default value "Probe"


        public int ReorderLevel { get; set; } = 0; // Minimum stock level

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("CreatedBy")]
        public string CreatedById { get; set; }
        public IdentityUser CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<StockRecord> StockRecords { get; set; }
        public ICollection<UsageRecord> UsageRecords { get; set; }


        // NEW - Add these 2 lines for Procedure Integration
        public ICollection<ProcedureItemAssignment> ProcedureAssignments { get; set; }
        public ICollection<ProcedureItemUsage> ProcedureUsages { get; set; }
    }
}
