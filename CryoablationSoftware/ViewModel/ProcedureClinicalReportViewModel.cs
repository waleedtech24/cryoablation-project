namespace CryoablationSoftware.ViewModel
{
    public class ProcedureClinicalReportViewModel
    {
        public Guid ClinicalReportId { get; set; }
        public string ReportType { get; set; }
        public string? ReportDescription { get; set; }
        public string ReportFilePath { get; set; }
        public DateTime ReportDate { get; set; }
        public DateTime LinkedAt { get; set; }
        public string LinkedByName { get; set; }
    }
}
