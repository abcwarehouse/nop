using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressModel : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int? ManufacturerId { get; set; }
        public string Comfort { get; set; }
        public int? ProductId { get; set; }
    }
}