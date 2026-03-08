namespace CryoablationSoftware.ViewModel
{
    public class InventoryDashboardViewModel
    {
        public int TotalItems { get; set; }
        public int TotalIprobes { get; set; }
        public int LowStockItems { get; set; }
        public int TotalSuppliers { get; set; }
        public decimal TotalInventoryValue { get; set; }

        // Added new metrics
        public int ActiveItems { get; set; }
        public int InactiveItems { get; set; }
        public int TotalCategories { get; set; }
        public int OutOfStockItems { get; set; }

        public int CurrentStock { get; set; }
        public int currentStockprobe { get; set; }
        public int UsedStock { get; set; }

        // Specific Item Type Metrics
        public int TotalProbes { get; set; }
        public int SterileCover { get; set; }
        public int Ethanol { get; set; }
        public int LiquidNitrogen { get; set; }


        public List<LowStockItemViewModel> LowStockAlerts { get; set; } = new List<LowStockItemViewModel>();
        public List<RecentStockMovementViewModel> RecentMovements { get; set; } = new List<RecentStockMovementViewModel>();

    }
}
