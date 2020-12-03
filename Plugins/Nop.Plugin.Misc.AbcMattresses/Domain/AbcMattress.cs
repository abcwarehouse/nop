using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattress : BaseEntity
    {
        public string Model { get; set; }
        public AbcMattressSize Size { get; set; }
        public string Brand { get; set; }
        public string Comfort { get; set; }
        public string MattressType { get; set; }
        
        // these two can also be merged
        public string Foundation { get; set; }
        public string AdjustableBase { get; set; }
        public int PackageItemNo { get; set; }
        public decimal PackagePrice { get; set; }
        public int MattressItemNo { get; set; }
        public decimal MattressPrice { get; set; }

        // can these be merged?
        public int? BaseItemNo { get; set; }
        public decimal BasePrice { get; set; }
        public int? AdjBaseItemNo { get; set; }
        public decimal AdjBasePrice { get; set; }
    }
}