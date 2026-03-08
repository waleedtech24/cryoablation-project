using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class StockRecord
    {
        [Key]
        public Guid StockRecordId { get; set; }

        public DateTime StockDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int QuantityReceived { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; } = 0;

        public DateTime? ExpiryDate { get; set; } // Optional for items with expiry

        public string? BatchNumber { get; set; } // Optional batch tracking

        public string? Notes { get; set; }

        // Foreign Keys
        [ForeignKey("Item")]
        public Guid ItemId { get; set; }
        public Item Item { get; set; }

        [ForeignKey("CreatedBy")]
        public string CreatedById { get; set; }
        public IdentityUser CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
