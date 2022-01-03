namespace AbcWarehouse.Plugin.Widgets.AddToCartSlideout.Models
{
    public record ProductInfoModel
    {
        public string ImageUrl { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
    }
}