namespace Nop.Plugin.Widgets.PowerReviews.Models
{
    public class ListingModel
    {
        public int ProductId { get; set; }
        public string ProductSku { get; set; }
        public PowerReviewsSettings Settings { get; set; }
    }
}
