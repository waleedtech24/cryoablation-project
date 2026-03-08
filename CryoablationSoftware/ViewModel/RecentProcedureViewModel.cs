namespace CryoablationSoftware.ViewModel
{
    public class RecentProcedureViewModel
    {
        public Guid ProcedureId { get; set; }
        public string PatientName { get; set; }
        public string ProcedureROI { get; set; }
        public DateTime ProcedureDate { get; set; }
        public string Status { get; set; }
        public int TotalItemsUsed { get; set; }
    }
}
