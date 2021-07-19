using System.Collections.Generic;

namespace Nop.Plugin.Widgets.PowerReviews.Models
{
    public class ListingScriptModel
    {
        public PowerReviewsSettings Settings { get; set; }
        public List<(int Id, string Sku, FeedlessProductModel feedlessProduct)> FeedlessProducts { get; set; }
    }
}
