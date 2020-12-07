using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressEntry : BaseEntity
    {
        public int AbcMattressModelId { get; set; }
        public string Size { get; set; }
        public int ItemNo { get; set; }
        public decimal Price { get; set; }
        public string Type { get; set; }
    }
}