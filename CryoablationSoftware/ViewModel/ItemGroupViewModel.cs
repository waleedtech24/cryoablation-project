namespace CryoablationSoftware.ViewModel
{
    public class ItemGroupViewModel
    {
        public string Category { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public int TotalCurrentStock { get; set; }
        public int TotalVariants { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsLowStock => TotalCurrentStock <= ReorderLevel;
        public bool IsOutOfStock => TotalCurrentStock <= 0;
        public bool HasActiveItems { get; set; }
        public DateTime LatestCreatedDate { get; set; }
        public string CreatedByUser { get; set; }

        public List<ItemVariantViewModel> Variants { get; set; } = new();
    }

    public class ItemVariantViewModel
    {
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsActive { get; set; }
        public string CreatedByUser { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsLowStock => CurrentStock > 0 && CurrentStock <= ReorderLevel;
        public bool IsOutOfStock => CurrentStock <= 0;
    }
}