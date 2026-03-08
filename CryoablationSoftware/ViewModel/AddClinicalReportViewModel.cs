using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class AddClinicalReportViewModel
    {
        public Guid ProcedureId { get; set; }
        public string PatientName { get; set; }
        public string ProcedureROI { get; set; }
        public DateTime ProcedureDate { get; set; }

        [Required(ErrorMessage = "Please select a clinical report")]
        public Guid ClinicalReportId { get; set; }

        // Available reports for selection
        public List<AvailableClinicalReportViewModel> AvailableReports { get; set; } = new List<AvailableClinicalReportViewModel>();

    }
}
