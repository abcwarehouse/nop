using System;

namespace Nop.Plugin.Widgets.PowerReviews.Models
{
    public class DetailModel
    {
        public string ManufacturerName { get; set; }
        public string ProductSku { get; set; }
        public DateTime PriceValidUntil { get; set; }
        public string ProductName { get; set; }
        public string MetaDescription { get; set; }
        public string ProductImageUrl { get; set; }
        public string ProductGtin { get; set; }
        public string ProductPrice { get; set; }
    }
}
