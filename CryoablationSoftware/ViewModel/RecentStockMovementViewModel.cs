namespace CryoablationSoftware.ViewModel
{
    public class RecentStockMovementViewModel
    {
        public DateTime Date { get; set; }
        public string ItemName { get; set; }
        public string MovementType { get; set; } // IN/OUT
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public string Reason { get; set; }

        // Additional properties for better display
        public string FormattedDate => Date.ToString("dd/MM/yyyy HH:mm");
        public string MovementIcon => MovementType == "IN" ? "fas fa-arrow-down" : "fas fa-arrow-up";
        public string MovementClass => MovementType == "IN" ? "success" : "danger";
    }
}
