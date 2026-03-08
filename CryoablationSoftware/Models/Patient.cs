using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using CryoablationSoftware.Models;
using Microsoft.AspNetCore.Identity;


namespace CryoablationSoftware.Models
{
    public class Patient
    {
        [Key]
        public Guid PatientId { get; set; }

        [Required]
        public string PatientName { get; set; }
        public DateTime? PatientDOB { get; set; }
        public string PatientCNIC { get; set; }
        public string PatientMRNumber { get; set; }
        public string PatientPhone { get; set; }
        public string PatientGender { get; set; }
        public int PatientAge { get; set; }
        public string PatientAddress { get; set; }
        public string PatientCity { get; set; }
        public string PatientProvince { get; set; }
        public string PatientInsuranceInfo { get; set; }
        public string PatientPaymentMode { get; set; }

        [ForeignKey("CreatedBy")]
        public string CreatedById { get; set; }
        public IdentityUser CreatedBy { get; set; }

        public ICollection<EmergencyContact> EmergencyContacts { get; set; }
        public ICollection<ReferralInfo> ReferralInfos { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
        public ICollection<MedicalRecord> MedicalRecords { get; set; }
        public ICollection<Procedure> Procedures { get; set; }

     

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;


    }
}
