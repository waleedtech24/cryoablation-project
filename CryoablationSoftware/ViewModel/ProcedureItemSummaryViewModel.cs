namespace CryoablationSoftware.ViewModel
{
    public class ProcedureItemSummaryViewModel
    {
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string Unit { get; set; }
        public int QuantityAssigned { get; set; }
        public int QuantityUsed { get; set; }
        public int RemainingQuantity => QuantityAssigned - QuantityUsed;
        public string AssignmentReason { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? LastUsageDate { get; set; }
        public string Status => GetStatus();

        private string GetStatus()
        {
            if (QuantityUsed == 0) return "Not Used";
            if (QuantityUsed == QuantityAssigned) return "Fully Used";
            if (QuantityUsed < QuantityAssigned) return "Partially Used";
            return "Over Used"; // Should not happen
        }
    }
}
