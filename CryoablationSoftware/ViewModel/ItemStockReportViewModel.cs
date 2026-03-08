namespace CryoablationSoftware.ViewModel
{
    public class ItemStockReportViewModel
    {
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime LastReceived { get; set; }
        public DateTime LastIssued { get; set; }
        public string Status => CurrentStock <= ReorderLevel ? "Low Stock" : "Normal";
    }
}
