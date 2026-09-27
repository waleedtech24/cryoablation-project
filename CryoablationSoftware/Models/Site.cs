using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class Site
    {
        [Key]
        public int SiteId { get; set; }

        [Required]
        [StringLength(300)]
        public string SiteName { get; set; }

        [StringLength(100)]
        public string SiteCity { get; set; }

        [StringLength(500)]
        public string? SiteAddress { get; set; }

        [StringLength(20)]
        public string? SitePhone { get; set; }

        [StringLength(100)]
        public string? SiteEmail { get; set; }

        public string? SiteDescription { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<UserProfile> UserProfiles { get; set; }
        public ICollection<Patient> Patients { get; set; }
        public ICollection<Procedure> Procedures { get; set; }
        public ICollection<Item> Items { get; set; }
    }
}