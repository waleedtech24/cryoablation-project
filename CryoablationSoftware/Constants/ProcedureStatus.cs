namespace CryoablationSoftware.Constants
{
    public static class ProcedureStatus
    {
        public const string Scheduled = "Scheduled";
        public const string InProgress = "In-Progress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static List<string> GetAllStatuses()
        {
            return new List<string> { Scheduled, InProgress, Completed, Cancelled };
        }
    }
}
