using System;
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

        public ProductAttributeValue ToProductAttributeValue(
            int productAttributeMappingId,
            decimal productPrice
        )
        {
            return new ProductAttributeValue()
            {
                ProductAttributeMappingId = productAttributeMappingId,
                Name = Size,
                PriceAdjustment = Price - productPrice,
                IsPreSelected = Size == "Queen",
                DisplayOrder = GetDisplayOrder(Size)
            };
        }

        private int GetDisplayOrder(string size)
        {
            switch (size)
            {
                case "Twin":
                    return 0;
                case "TwinXL":
                    return 10;
                case "Full":
                    return 20;
                case "Queen":
                    return 30;
                case "King":
                    return 40;
                case "California King":
                    return 50;
                default:
                    throw new ArgumentException("Invalid mattress size provided.");
            }
        }
    }
}