using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressPackage : BaseEntity
    {
        public int AbcMattressEntryId { get; set; }
        public int AbcMattressBaseId { get; set; }
        public string ItemNo { get; set; }
        public decimal Price { get; set; }
    }
}