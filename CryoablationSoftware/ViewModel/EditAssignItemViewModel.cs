namespace CryoablationSoftware.ViewModel
{
    public class EditAssignItemViewModel
    {
        public Guid? AssignmentId { get; set; }
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    // NEW - Main page ViewModel
    public class EditAssignedItemsPageViewModel
    {
        public Guid ProcedureId { get; set; }
        public string PatientName { get; set; }
        public string ProcedureROI { get; set; }
        public string ProcedureStatus { get; set; }

        // For dropdowns
        public List<ItemDropdownViewModel> AvailableItems { get; set; } = new List<ItemDropdownViewModel>();

        // Existing assigned items
        public List<ExistingAssignedItemViewModel> ExistingItems { get; set; } = new List<ExistingAssignedItemViewModel>();
    }

    public class ItemDropdownViewModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public string Unit { get; set; }
    }

    public class ExistingAssignedItemViewModel
    {
        public string AssignmentId { get; set; }
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; }
        public string Unit { get; set; }
    }
}
