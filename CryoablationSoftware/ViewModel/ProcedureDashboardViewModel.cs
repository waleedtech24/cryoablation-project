namespace CryoablationSoftware.ViewModel
{
    public class ProcedureDashboardViewModel
    {
        public int TotalProcedures { get; set; }
        public int ScheduledProcedures { get; set; }
        public int InProgressProcedures { get; set; }
        public int CompletedProcedures { get; set; }
        public int CancelledProcedures { get; set; }

        public List<UpcomingProcedureViewModel> UpcomingProcedures { get; set; } = new List<UpcomingProcedureViewModel>();
        public List<RecentProcedureViewModel> RecentProcedures { get; set; } = new List<RecentProcedureViewModel>();
        public List<ProcedureItemUsageSummaryViewModel> ItemUsageSummary { get; set; } = new List<ProcedureItemUsageSummaryViewModel>();

    }
}
