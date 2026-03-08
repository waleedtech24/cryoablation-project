namespace CryoablationSoftware.Services
{
    public interface IAuditLogService
    {
        // Custom log with message
        Task LogAsync(
            string actionType,
            string message,
            string? recordId = null,
            string? tableName = null,
            object? additionalData = null,
            string? serviceName = null,
            string? methodName = null);

        // Exception logging
        Task LogExceptionAsync(
            Exception exception,
            string? methodName = null,
            string? customMessage = null,
            object? additionalData = null,
            string? serviceName = null);

        // Database operations
        Task LogDatabaseOperationAsync(
            string actionType,
            string tableName,
            string recordId,
            object? oldValues = null,
            object? newValues = null,
            string? customMessage = null);

        // Login/Logout
        Task LogLoginAsync(
            string userId,
            string userName,
            string userRole,
            bool isSuccessful,
            string? errorMessage = null);

        Task LogLogoutAsync(string userId, string userName);
    }
}
