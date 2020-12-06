using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressGift : BaseEntity
    {
        public string Model { get; set; }
        public int ItemNo { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}