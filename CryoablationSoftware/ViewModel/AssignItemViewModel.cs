namespace CryoablationSoftware.ViewModel
{
    // Single item assignment
    public class AssignItemViewModel
    {
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; }
    }

    // Response for assignment operation
    public class AssignmentResultViewModel
    {
        public bool Success { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<string> SuccessMessages { get; set; } = new List<string>();
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}