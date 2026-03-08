using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class ProcedureItemUsageViewModel
    {

        public Guid ProcedureId { get; set; }
        public string? PatientName { get; set; }
        public string? ProcedureROI { get; set; }
        public DateTime ProcedureDate { get; set; }

        [Required(ErrorMessage = "Please select an item")]
        public Guid ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemCode { get; set; }
        public string? Unit { get; set; }

        // Assignment Info
        public int QuantityAssigned { get; set; }
        public int PreviouslyUsed { get; set; }
        public int RemainingAssigned => QuantityAssigned - PreviouslyUsed;

        [Required(ErrorMessage = "Quantity used is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int QuantityUsed { get; set; }

        [Required(ErrorMessage = "Usage date/time is required")]
        public DateTime UsageDateTime { get; set; } = DateTime.Now;

        public string? UsageNotes { get; set; }
    }
}
