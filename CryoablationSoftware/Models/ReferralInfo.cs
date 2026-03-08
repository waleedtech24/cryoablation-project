using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class ReferralInfo
    {

        [Key]
        public Guid ReferralInfoId { get; set; }

        public string ReferralHospitalName { get; set; }
        public string? ReferralCity { get; set; }
        public string? ReferralDoctorName { get; set; }
        public string? ReferralDepartment { get; set; }
        public string? ReferralPhone { get; set; }
        public string? ReferralEmail { get; set; }

        [ForeignKey("Patient")]
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
