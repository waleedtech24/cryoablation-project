using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModels
{
    // List pe dikhane ke liye
    public class SiteListViewModel
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public string? SiteCity { get; set; }
        public string? SitePhone { get; set; }
        public string? SiteEmail { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPatients { get; set; }
    }

    // Create / Edit form ke liye
    public class SiteFormViewModel
    {
        public int SiteId { get; set; } // 0 = Create, >0 = Edit

        [Required(ErrorMessage = "Site name is required")]
        [StringLength(200, ErrorMessage = "Site name cannot exceed 200 characters")]
        [Display(Name = "Site Name")]
        public string SiteName { get; set; }

        [StringLength(100)]
        [Display(Name = "City")]
        public string? SiteCity { get; set; }

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? SiteAddress { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone")]
        public string? SitePhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? SiteEmail { get; set; }

        [Display(Name = "Description")]
        public string? SiteDescription { get; set; }

        public bool IsActive { get; set; } = true;
    }
}