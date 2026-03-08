using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class ItemCreateViewModel
    {
        [Required(ErrorMessage = "Item Name is required")]
        [StringLength(100, ErrorMessage = "Item Name cannot exceed 100 characters")]
        public string ItemName { get; set; }

        [Required(ErrorMessage = "Item Code is required")]
        [StringLength(500, ErrorMessage = "Item Code cannot exceed 50 characters")]
        public string ItemCode { get; set; }

        [Required(ErrorMessage = "Item Type is required")]
        public string ItemType { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Unit is required")]
        public string Unit { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Reorder Level must be 0 or greater")]
        public int ReorderLevel { get; set; } = 0;

        public string Description { get; set; }
    }
}
