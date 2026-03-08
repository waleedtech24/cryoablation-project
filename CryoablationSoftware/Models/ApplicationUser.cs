using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string Role { get; set; } // Admin, Nurse, Doctor

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public string Designation { get; set; } // optional
    }
}
