using CryoablationSoftware.Data;
using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CryoablationSoftware.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserManagementService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<UserListViewModel>> GetAllUsersAsync()
        {
            var profiles = await _context.UserProfiles
    .Include(u => u.Site)
    .Where(u => u.UserRole != "Super Admin" && u.UserRole!= "SuperAdmin2")   // Super Admin hide karo
    .OrderBy(u => u.FullName)
    .ToListAsync();

            var result = new List<UserListViewModel>();

            foreach (var profile in profiles)
            {
                var identityUser = await _userManager.FindByIdAsync(profile.UserId);
                if (identityUser == null) continue;

                result.Add(new UserListViewModel
                {
                    UserId = profile.UserId,
                    FullName = profile.FullName,
                    Email = identityUser.Email,
                    UserName = identityUser.UserName,
                    UserRole = profile.UserRole,
                    Designation = profile.Designation,
                    SiteId = profile.SiteId,
                    SiteName = profile.Site?.SiteName ?? "Super Admin",
                    IsActive = profile.IsActive,
                    CreatedAt = profile.CreatedAt
                });
            }

            return result;
        }

        public async Task<UserFormViewModel> GetUserForEditAsync(string userId)
        {
            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (profile == null) return null;

            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null) return null;

            return new UserFormViewModel
            {
                UserId = userId,
                FullName = profile.FullName,
                Email = identityUser.Email,
                UserName = identityUser.UserName,
                UserRole = profile.UserRole,
                Designation = profile.Designation,
                SiteId = profile.SiteId
            };
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(UserFormViewModel model)
        {
            try
            {
                // Email duplicate check
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                    return (false, $"Email '{model.Email}' is already registered.");

                // Super Admin ka SiteId null hona chahiye
                if (model.UserRole == "Super Admin")
                    model.SiteId = null;
                else if (model.SiteId == null)
                    return (false, "Please select a site for this user.");

                // Create Identity User
                var identityUser = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(identityUser, model.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return (false, $"Error creating user: {errors}");
                }

                // Assign Role
                await _userManager.AddToRoleAsync(identityUser, model.UserRole);

                // Create UserProfile
                var profile = new UserProfile
                {
                    UserId = identityUser.Id,
                    FullName = model.FullName.Trim(),
                    UserRole = model.UserRole,
                    Designation = model.Designation?.Trim(),
                    SiteId = model.SiteId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                return (true, $"User '{model.FullName}' created successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(UserFormViewModel model)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(u => u.UserId == model.UserId);

                if (profile == null)
                    return (false, "User profile not found.");

                var identityUser = await _userManager.FindByIdAsync(model.UserId);
                if (identityUser == null)
                    return (false, "User not found.");

                // Email change ho toh check karo
                if (identityUser.Email != model.Email)
                {
                    var emailExists = await _userManager.FindByEmailAsync(model.Email);
                    if (emailExists != null)
                        return (false, $"Email '{model.Email}' is already in use.");

                    identityUser.Email = model.Email;
                    identityUser.UserName = model.Email;
                    await _userManager.UpdateAsync(identityUser);
                }

                // Role update
                var currentRoles = await _userManager.GetRolesAsync(identityUser);
                if (!currentRoles.Contains(model.UserRole))
                {
                    await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                    await _userManager.AddToRoleAsync(identityUser, model.UserRole);
                }

                // Super Admin ka SiteId null
                if (model.UserRole == "Super Admin")
                    model.SiteId = null;

                // Profile update
                profile.FullName = model.FullName.Trim();
                profile.UserRole = model.UserRole;
                profile.Designation = model.Designation?.Trim();
                profile.SiteId = model.SiteId;

                await _context.SaveChangesAsync();

                return (true, $"User '{model.FullName}' updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleUserStatusAsync(string userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (profile == null)
                    return (false, "User not found.");

                profile.IsActive = !profile.IsActive;
                await _context.SaveChangesAsync();

                var status = profile.IsActive ? "activated" : "deactivated";
                return (true, $"User '{profile.FullName}' {status} successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string userId, string newPassword)
        {
            try
            {
                var identityUser = await _userManager.FindByIdAsync(userId);
                if (identityUser == null)
                    return (false, "User not found.");

                var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                var result = await _userManager.ResetPasswordAsync(identityUser, token, newPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Password reset failed: {errors}");
                }

                return (true, "Password reset successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }
}