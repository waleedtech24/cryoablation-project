using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class ReceiveVm
    {
        [Required] public Guid ItemId { get; set; }
        [Required] public decimal Qty { get; set; }
        public decimal? UnitCost { get; set; }
        public string BatchNo { get; set; }
        public string Notes { get; set; }
    }
}
