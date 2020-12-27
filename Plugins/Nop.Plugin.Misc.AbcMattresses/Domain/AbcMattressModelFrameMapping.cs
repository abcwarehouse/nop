using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressModelFrameMapping : BaseEntity
    {
        public int AbcMattressModelId { get; set; }
        public int AbcMattressFrameId { get; set; }
    }
}