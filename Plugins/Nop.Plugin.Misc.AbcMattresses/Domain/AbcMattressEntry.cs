using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressEntry : BaseEntity
    {
        public int AbcMattressModelId { get; set; }
        public string Size { get; set; }
        public string ItemNo { get; set; }
        public decimal Price { get; set; }
        public string Type { get; set; }

        public ProductAttributeValue ToProductAttributeValue(int productAttributeMappingId)
        {
            return new ProductAttributeValue()
            {
                ProductAttributeMappingId = productAttributeMappingId,
                Name = Size,
                PriceAdjustment = Price
            };
        }
    }
}