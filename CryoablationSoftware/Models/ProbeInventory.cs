using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class ProbeInventory
    {
        [Key]
        public Guid ProbeId { get; set; }

        public string ProbeModel { get; set; }
        public string ProbeSerialNumber { get; set; }
        public DateTime ProbeExpiryDate { get; set; }
        public string ProbeType { get; set; }
        public string ProbeSize { get; set; }
        public string ProbeManufacturer { get; set; }
        public string ProbeStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
