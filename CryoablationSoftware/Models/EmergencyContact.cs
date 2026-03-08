using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class EmergencyContact
    {
        [Key]
        public Guid EmergencyContactId { get; set; }

        public string EmergencyContactName { get; set; }
        public string EmergencyContactCNIC { get; set; }
        public string EmergencyContactRelationship { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string EmergencyContactAddress { get; set; }
        public string EmergencyContactCity { get; set; }
        public string EmergencyContactProvince { get; set; }

        [ForeignKey("Patient")]
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
