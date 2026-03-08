using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class Appointment
    {

        [Key]
        public Guid AppointmentId { get; set; }

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; }

        public DateTime AppointmentDateTime { get; set; }
        public string AppointmentClinic { get; set; }
        public string AppointmentDoctor { get; set; }
        public string AppointmentMode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
