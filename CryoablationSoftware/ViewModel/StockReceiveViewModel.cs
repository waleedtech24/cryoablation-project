using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class StockReceiveViewModel
    {
        [Required(ErrorMessage = "Please select an item")]
        public Guid ItemId { get; set; }

        public string ItemName { get; set; } // For display

        [Required(ErrorMessage = "Stock Date is required")]
        public DateTime StockDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int QuantityReceived { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Unit Cost must be 0 or greater")]
        public decimal UnitCost { get; set; } = 0;

        public decimal TotalCost => QuantityReceived * UnitCost;

        public DateTime? ExpiryDate { get; set; }

        public string? BatchNumber { get; set; }

        //public Guid? SupplierId { get; set; }

        //public string SupplierName { get; set; } // For display

        public string? Notes { get; set; }
    }
}
