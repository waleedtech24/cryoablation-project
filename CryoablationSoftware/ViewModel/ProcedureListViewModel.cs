namespace CryoablationSoftware.ViewModel
{
    public class ProcedureListViewModel
    {
        public Guid ProcedureId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientMRNumber { get; set; }
        public DateTime ProcedureDate { get; set; }
        public string ProcedureROI { get; set; }
        public string ProcedureDuration { get; set; }
        public string ProcedureStatus { get; set; }
        public int TotalItemsAssigned { get; set; }
        public int TotalItemsUsed { get; set; }
        public string CreatedByName { get; set; }
        public string PrimaryDoctorName { get; set; }

        public int ItemsAssigned { get; set; }
        public int ItemsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
