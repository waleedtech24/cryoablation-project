using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class ProcedureItemAssignmentViewModel
    {
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string Unit { get; set; }
        public int AvailableStock { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int QuantityAssigned { get; set; }

        // REMOVED: AssignmentReason - no longer required
        public string AssignmentReason { get; set; } = "Procedure Requirement"; // Hidden default value

        // FIXED: Made Notes Optional
        public string? Notes { get; set; }
    }
}
