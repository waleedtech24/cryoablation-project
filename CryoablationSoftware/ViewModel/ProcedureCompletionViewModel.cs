using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class ProcedureCompletionViewModel
    {
        public Guid ProcedureId { get; set; }
        public string? PatientName { get; set; }
        public string? ProcedureROI { get; set; }
        public DateTime ProcedureDate { get; set; }

        [Required(ErrorMessage = "IR Consultant remarks are required")]
        public string IRConsultantRemarks { get; set; }

        public DateTime? DischargeDate { get; set; }
        public DateTime? NextProcedureDate { get; set; }

        public string? CompletionNotes { get; set; }

    }
}
