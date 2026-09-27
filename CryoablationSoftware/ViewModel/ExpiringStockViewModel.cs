namespace CryoablationSoftware.ViewModel
{
    public class ExpiringStockViewModel
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string Unit { get; set; }
        public int QuantityReceived { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
    }
}
