namespace CryoablationSoftware.ViewModel
{
    public class AvailableClinicalReportViewModel
    {
        public Guid ClinicalReportId { get; set; }
        public string ReportType { get; set; }
        public string? ReportDescription { get; set; }
        public DateTime ReportDate { get; set; }
        public bool IsAlreadyLinked { get; set; }
    }
}
