namespace CryoablationSoftware.ViewModel
{
    public class UpcomingProcedureViewModel
    {
        public Guid ProcedureId { get; set; }
        public string PatientName { get; set; }
        public string ProcedureROI { get; set; }
        public DateTime ProcedureDate { get; set; }
        public string Status { get; set; }
        public int DaysUntilProcedure => (ProcedureDate.Date - DateTime.Today).Days;
    }
}
