using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressModelGiftMapping : BaseEntity
    {
        public int AbcMattressModelId { get; set; }
        public int AbcMattressGiftId { get; set; }
    }
}