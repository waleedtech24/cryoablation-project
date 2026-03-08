using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class LiquidNitrogenLog
    {
        [Key]
        public Guid LiquidNitrogenLogId { get; set; }

        [ForeignKey("Procedure")]
        public Guid ProcedureId { get; set; }
        public Procedure Procedure { get; set; }

        public float LiquidNitrogenUsedLiters { get; set; }
        public string LiquidNitrogenRemarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
