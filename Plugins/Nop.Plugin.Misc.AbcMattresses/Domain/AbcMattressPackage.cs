using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressPackage : BaseEntity
    {
        public int MattressItemNo { get; set; }
        public int BaseItemNo { get; set; }
        public int ItemNo { get; set; }
        public decimal Price { get; set; }
    }
}