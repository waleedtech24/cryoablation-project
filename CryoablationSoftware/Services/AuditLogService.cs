using CryoablationSoftware.Data;
using CryoablationSoftware.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace CryoablationSoftware.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;

        public AuditLogService(
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task LogAsync(
            string actionType,
            string message,
            string? recordId = null,
            string? tableName = null,
            object? additionalData = null,
            string? serviceName = null,
            string? methodName = null)
        {
            var log = CreateBaseLog();
            log.LogType = "CUSTOM";
            log.ActionType = actionType;
            log.CustomMessage = message;
            log.RecordId = recordId;
            log.TableName = tableName;
            log.ServiceName = serviceName;
            log.MethodName = methodName;
            log.IsSuccessful = true;

            if (additionalData != null)
            {
                log.AdditionalData = SerializeToJson(additionalData);
            }

            await SaveLogAsync(log);
        }

        public async Task LogExceptionAsync(
            Exception exception,
            string? methodName = null,
            string? customMessage = null,
            object? additionalData = null,
            string? serviceName = null)
        {
            var log = CreateBaseLog();
            log.LogType = "EXCEPTION";
            log.ActionType = "EXCEPTION_OCCURRED";
            log.IsSuccessful = false;
            log.MethodName = methodName;
            log.ServiceName = serviceName;
            log.CustomMessage = customMessage ?? "An error occurred";
            log.ExceptionMessage = exception.Message;
            log.ExceptionType = exception.GetType().Name;
            log.ExceptionStackTrace = exception.StackTrace;

            if (exception.InnerException != null)
            {
                log.InnerException = $"{exception.InnerException.Message}\n{exception.InnerException.StackTrace}";
            }

            if (additionalData != null)
            {
                log.AdditionalData = SerializeToJson(additionalData);
            }

            await SaveLogAsync(log);
        }

        public async Task LogDatabaseOperationAsync(
            string actionType,
            string tableName,
            string recordId,
            object? oldValues = null,
            object? newValues = null,
            string? customMessage = null)
        {
            var log = CreateBaseLog();
            log.LogType = "DATABASE";
            log.ActionType = actionType;
            log.TableName = tableName;
            log.RecordId = recordId;
            log.CustomMessage = customMessage ?? $"{actionType} operation on {tableName}";
            log.IsSuccessful = true;

            if (oldValues != null)
            {
                log.OldValues = SerializeToJson(oldValues);
            }

            if (newValues != null)
            {
                log.NewValues = SerializeToJson(newValues);
            }

            await SaveLogAsync(log);
        }

        public async Task LogLoginAsync(
            string userId,
            string userName,
            string userRole,
            bool isSuccessful,
            string? errorMessage = null)
        {
            var log = CreateBaseLog();
            log.LogType = "LOGIN";
            log.ActionType = isSuccessful ? "LOGIN_SUCCESS" : "LOGIN_FAILED";
            log.UserId = userId;
            log.UserName = userName;
            log.UserRole = userRole;
            log.IsSuccessful = isSuccessful;
            log.CustomMessage = isSuccessful
                ? $"User {userName} logged in successfully"
                : $"Failed login attempt for {userName}: {errorMessage}";

            await SaveLogAsync(log);
        }

        public async Task LogLogoutAsync(string userId, string userName)
        {
            var log = CreateBaseLog();
            log.LogType = "LOGOUT";
            log.ActionType = "LOGOUT_SUCCESS";
            log.UserId = userId;
            log.UserName = userName;
            log.IsSuccessful = true;
            log.CustomMessage = $"User {userName} logged out";

            await SaveLogAsync(log);
        }

        private AuditLog CreateBaseLog()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var log = new AuditLog
            {
                CreatedAt = DateTime.UtcNow
            };

            if (httpContext != null)
            {
                var user = httpContext.User;
                if (user.Identity?.IsAuthenticated == true)
                {
                    log.UserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    log.UserName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.Email);
                    log.UserRole = user.FindFirstValue(ClaimTypes.Role);
                }

                log.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                log.HttpMethod = httpContext.Request.Method;
                log.RequestPath = httpContext.Request.Path;
                log.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();

                var routeData = httpContext.GetRouteData();
                if (routeData != null)
                {
                    log.ControllerName = routeData.Values["controller"]?.ToString();
                    log.ActionName = routeData.Values["action"]?.ToString();
                }
            }

            return log;
        }

        private async Task SaveLogAsync(AuditLog log)
        {
            try
            {
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception dbException)
            {
                // Fallback: File mein save karega agar DB fail ho
                await SaveLogToFileAsync(log, dbException);
            }
        }

        private async Task SaveLogToFileAsync(AuditLog log, Exception originalException)
        {
            try
            {
                // FailedAuditLogs folder banao agar nahi hai
                var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "FailedAuditLogs");
                Directory.CreateDirectory(logDirectory);

                // File name with date
                var fileName = $"audit_{DateTime.UtcNow:yyyyMMdd}.log";
                var filePath = Path.Combine(logDirectory, fileName);

                // Complete log entry format
                var logEntry = $@"
=== AUDIT LOG (DATABASE SAVE FAILED) ===
Timestamp: {log.CreatedAt:yyyy-MM-dd HH:mm:ss}
User: {log.UserName} ({log.UserId})
Role: {log.UserRole}
Controller: {log.ControllerName}
Action: {log.ActionName}
Service: {log.ServiceName}
Method: {log.MethodName}
ActionType: {log.ActionType}
LogType: {log.LogType}
TableName: {log.TableName}
RecordId: {log.RecordId}
IsSuccessful: {log.IsSuccessful}
IP Address: {log.IpAddress}
HTTP Method: {log.HttpMethod}
Request Path: {log.RequestPath}
User Agent: {log.UserAgent}
Custom Message: {log.CustomMessage}
Additional Data: {log.AdditionalData}
Old Values: {log.OldValues}
New Values: {log.NewValues}
Exception Message: {log.ExceptionMessage}
Exception Type: {log.ExceptionType}
Exception StackTrace: {log.ExceptionStackTrace}
Inner Exception: {log.InnerException}
Duration: {log.Duration}ms

DATABASE ERROR:
Error Type: {originalException.GetType().Name}
Error Message: {originalException.Message}
Error StackTrace: {originalException.StackTrace}
========================================

";

                // File mein append karo
                await File.AppendAllTextAsync(filePath, logEntry);
            }
            catch (Exception fileException)
            {
                // Last resort - at least console mein print ho
                System.Diagnostics.Debug.WriteLine($"CRITICAL AUDIT FAILURE");
                System.Diagnostics.Debug.WriteLine($"Action: {log.ActionType}");
                System.Diagnostics.Debug.WriteLine($"DB Error: {originalException.Message}");
                System.Diagnostics.Debug.WriteLine($"File Error: {fileException.Message}");

                Console.WriteLine($"[CRITICAL] Complete audit log failure for action: {log.ActionType}");
            }
        }

        private string SerializeToJson(object obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return obj?.ToString() ?? string.Empty;
            }
        }
    }
}