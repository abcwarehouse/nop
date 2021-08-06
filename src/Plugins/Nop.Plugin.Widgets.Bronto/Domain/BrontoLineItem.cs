namespace Nop.Plugin.Widgets.Bronto.Domain
{
    public record BrontoLineItem(
        string Sku,
        string Name,
        string Description,
        string Category,
        string Other,
        decimal UnitPrice,
        decimal SalePrice,
        int Quantity,
        decimal TotalPrice,
        string ImageUrl,
        string ProductUrl
    );
}
