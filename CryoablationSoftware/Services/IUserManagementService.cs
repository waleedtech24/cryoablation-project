using CryoablationSoftware.ViewModels;

namespace CryoablationSoftware.Services
{
    public interface IUserManagementService
    {
        Task<List<UserListViewModel>> GetAllUsersAsync();
        Task<UserFormViewModel> GetUserForEditAsync(string userId);
        Task<(bool Success, string Message)> CreateUserAsync(UserFormViewModel model);
        Task<(bool Success, string Message)> UpdateUserAsync(UserFormViewModel model);
        Task<(bool Success, string Message)> ToggleUserStatusAsync(string userId);
        Task<(bool Success, string Message)> ResetPasswordAsync(string userId, string newPassword);
    }
}