using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Plugin.Misc.AbcSync.Domain
{
    [Table("dbo.PhysicalStoreStock")]
    public partial class PhysicalStoreStock
    {
        public int Id { get; set; }
        public string BackendBranchId { get; set; }
        public string ItemSku { get; set; }
        public int Quantity { get; set; }
    }
}