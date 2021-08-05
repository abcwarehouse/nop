using System;

namespace Nop.Plugin.Widgets.PowerReviews.Models
{
    public class DetailModel
    {
        public string ProductSku { get; set; }
        public int ProductId { get; set; }
        public string ProductPrice { get; set; }
        public DateTime PriceValidUntil { get; set; }
        public PowerReviewsSettings Settings { get; set; }
        public FeedlessProductModel FeedlessProduct { get; set; }

    }
}
