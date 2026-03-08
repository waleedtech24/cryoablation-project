namespace CryoablationSoftware.ViewModel
{
    public class PatientProceduresViewModel
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientMRNumber { get; set; }
        public string? PatientPhone { get; set; }
        public int? PatientAge { get; set; }
        public string? PatientGender { get; set; }

        public List<ProcedureListViewModel> Procedures { get; set; } = new List<ProcedureListViewModel>();

        // Summary Stats
        public int TotalProcedures => Procedures.Count;
        public int CompletedProcedures => Procedures.Count(p => p.ProcedureStatus == "Completed");
        public int ScheduledProcedures => Procedures.Count(p => p.ProcedureStatus == "Scheduled");
        public int InProgressProcedures => Procedures.Count(p => p.ProcedureStatus == "In-Progress");
        public int CancelledProcedures => Procedures.Count(p => p.ProcedureStatus == "Cancelled");
    }
}
