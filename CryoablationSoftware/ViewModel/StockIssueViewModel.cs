using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class StockIssueViewModel
    {
        [Required(ErrorMessage = "Please select an item")]
        public Guid ItemId { get; set; }

        public string? ItemName { get; set; } // For display
        public int AvailableStock { get; set; } // For display

        [Required(ErrorMessage = "Usage Date is required")]
        public DateTime UsageDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int QuantityUsed { get; set; }

        [Required(ErrorMessage = "Usage Reason is required")]
        public string UsageReason { get; set; } // Procedure, Maintenance, Damaged, etc.

        public Guid? PatientId { get; set; }
        public string? PatientName { get; set; } // For display

        public Guid? ProcedureId { get; set; }

        public string? Notes { get; set; }
    }
}
