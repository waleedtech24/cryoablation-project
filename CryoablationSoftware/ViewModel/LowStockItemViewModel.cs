namespace CryoablationSoftware.ViewModel
{
    public class LowStockItemViewModel
    {
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public string Unit { get; set; }

        // Additional properties for better display
        public bool IsOutOfStock => CurrentStock <= 0;
        public bool IsLowStock => CurrentStock > 0 && CurrentStock <= ReorderLevel;
    }
}
