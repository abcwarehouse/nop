namespace Nop.Plugin.Misc.AbcFrontend.Models
{
    public record AddToCartSlideoutInfo
    {
        public string ProductName { get; init; }
        public string ProductDescription { get; init; }
        public string ProductPictureUrl { get; init; }
        public bool IsAbcDeliveryItem { get; init; }
        public decimal Subtotal { get; init; }
    }
}
