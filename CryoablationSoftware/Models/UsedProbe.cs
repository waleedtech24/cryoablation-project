using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class UsedProbe
    {
        [Key]
        public Guid UsedProbeId { get; set; }

        [ForeignKey("Procedure")]
        public Guid ProcedureId { get; set; }
        public Procedure Procedure { get; set; }

        [ForeignKey("ProbeInventory")]
        public Guid ProbeId { get; set; }
        public ProbeInventory ProbeInventory { get; set; }

        public int UsedProbeQuantity { get; set; }
        public int ReturnedProbeQuantity { get; set; }
        public string UsedProbeRemarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
