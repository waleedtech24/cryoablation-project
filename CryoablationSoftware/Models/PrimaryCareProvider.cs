using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class PrimaryCareProvider
    {
        [Key]
        public Guid PrimaryCareProviderId { get; set; }

        public string PrimaryCareProviderName { get; set; }
        public string PrimaryCareProviderDepartment { get; set; }
        public string PrimaryCareProviderEmail { get; set; }
        public string PrimaryCareProviderPhone { get; set; }

        [ForeignKey("Patient")]
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; }
    }
}
