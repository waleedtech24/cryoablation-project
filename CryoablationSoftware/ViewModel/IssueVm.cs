using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class IssueVm
    {
        [Required] public Guid ItemId { get; set; }
        [Required] public decimal Qty { get; set; }
        public Guid? ProcedureId { get; set; }
        public string Notes { get; set; }
    }
}
