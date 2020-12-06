using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattress : BaseEntity
    {
        public string Model { get; set; }
        public string Brand { get; set; }
        public string Comfort { get; set; }
    }
}