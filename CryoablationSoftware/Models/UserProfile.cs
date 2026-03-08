// Models/UserProfile.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class UserProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // IdentityUser.Id

        [Required]
        public string FullName { get; set; }

        [Required]
        public string UserRole { get; set; } // Super Admin, Admin, Manager

        public string? Designation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}