using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class StockTransaction
    {
        [Key] public Guid StockTransactionId { get; set; }

        [Required] public Guid ItemId { get; set; }
        public Item Item { get; set; }

        public DateTime TranDate { get; set; } = DateTime.UtcNow;

        // Allowed: IN, OUT, RETURN, DISPOSE, ADJ
        [Required, MaxLength(15)]
        public string TranType { get; set; }

        // Always store positive numbers; business logic applies sign
        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Qty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }   // relevant for IN/RETURN

        [MaxLength(50)]
        public string BatchNo { get; set; }      // optional; keep generic for now

        [MaxLength(20)]
        public string RefType { get; set; }      // e.g., PROCEDURE, PO, AUDIT

        public Guid? RefId { get; set; }         // e.g., ProcedureId

        [MaxLength(300)]
        public string Notes { get; set; }

        [Required]
        public Guid CreatedById { get; set; }    // ApplicationUser.Id (Guid)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
