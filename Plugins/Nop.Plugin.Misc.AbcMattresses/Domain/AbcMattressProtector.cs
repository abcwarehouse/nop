using System;
using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Misc.AbcMattresses.Domain
{
    public class AbcMattressProtector : BaseEntity
    {
        public string Size { get; set; }
        public string ItemNo { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        public ProductAttributeValue ToProductAttributeValue(int productAttributeMappingId)
        {
            return new ProductAttributeValue()
            {
                ProductAttributeMappingId = productAttributeMappingId,
                Name = Name,
                PriceAdjustment = Price
            };
        }
    }
}