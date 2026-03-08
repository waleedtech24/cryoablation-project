using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.Models
{
    public class AuditLog
    {
        [Key]
        public long LogId { get; set; }

        // User Context
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }

        // Log Classification
       
        public string? LogType { get; set; } // CUSTOM, DATABASE, EXCEPTION, LOGIN, LOGOUT

      
        public string? ActionType { get; set; } // INSERT, UPDATE, DELETE, PROCEDURE_CREATE, etc.

   
        public bool IsSuccessful { get; set; }

        // Entity Tracking
        
        public string? TableName { get; set; }

  
        public string? RecordId { get; set; }

        // Request Context
      
        public string? ControllerName { get; set; }

      
        public string? ActionName { get; set; }

      
        public string? ServiceName { get; set; }

       
        public string? MethodName { get; set; }

      
        public string? HttpMethod { get; set; }

      
        public string? RequestPath { get; set; }

       
        public string? IpAddress { get; set; }

   
        public string? UserAgent { get; set; }

        // Messages & Details
        public string? CustomMessage { get; set; }
        public string? AdditionalData { get; set; } // JSON

        // Change Tracking
        public string? OldValues { get; set; } // JSON
        public string? NewValues { get; set; } // JSON

        // Exception Details
        public string? ExceptionMessage { get; set; }

       
        public string? ExceptionType { get; set; }

        public string? ExceptionStackTrace { get; set; }
        public string? InnerException { get; set; }

        // Performance
        public int? Duration { get; set; } // Milliseconds

        // Timestamp
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
