using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressBase : BaseEntity
    {
        public int ItemNo { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public bool IsAdjustable { get; set; }
    }
}