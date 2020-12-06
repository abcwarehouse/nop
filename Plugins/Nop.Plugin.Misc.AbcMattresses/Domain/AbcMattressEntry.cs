using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressEntry : BaseEntity
    {
        public string Model { get; set; }
        public string Size { get; set; }
        public int MattressItemNo { get; set; }
        public decimal MattressPrice { get; set; }
        public string MattressType { get; set; }
    }
}