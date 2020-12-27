using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressFrame : BaseEntity
    {
        public string ItemNo { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}