using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModels
{
    public class UserListViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string UserRole { get; set; }
        public string? Designation { get; set; }
        public int? SiteId { get; set; }
        public string SiteName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserFormViewModel
    {
        public string? UserId { get; set; } // null = Create, value = Edit

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(200)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Username")]
        public string? UserName { get; set; }

        // Password sirf Create pe required hoga
        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string UserRole { get; set; }

        [StringLength(200)]
        [Display(Name = "Designation")]
        public string? Designation { get; set; }

        [Display(Name = "Site")]
        public int? SiteId { get; set; }

        // Dropdown ke liye
        public List<SiteDropdownItem> AvailableSites { get; set; } = new();
    }

    public class SiteDropdownItem
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public string SiteCity { get; set; }
    }

    public class ResetPasswordViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm the password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
    }
}