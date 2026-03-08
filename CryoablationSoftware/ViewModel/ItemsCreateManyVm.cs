using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class ItemsCreateManyVm
    {
        public List<ItemRowVm> Rows { get; set; } = new();
    }

    public class ItemRowVm
    {
        [Required, MaxLength(150)]
        public string ItemName { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        [Required, MaxLength(20)]
        public string Unit { get; set; }

        [Range(0, 999999)]
        public decimal? ReorderLevel { get; set; }
    }
}
