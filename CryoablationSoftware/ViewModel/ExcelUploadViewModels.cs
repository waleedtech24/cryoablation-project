namespace CryoablationSoftware.ViewModel
{
    public class ExcelUploadViewModels
    {
        public int TotalRowsProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> SuccessfulItems { get; set; }
        public List<ExcelUploadErrorViewModel> FailedItems { get; set; }
    }
    public class ExcelUploadErrorViewModel
    {
        public int RowNumber { get; set; }
        public string SerialNumber { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public List<string> Errors { get; set; }
    }
}
