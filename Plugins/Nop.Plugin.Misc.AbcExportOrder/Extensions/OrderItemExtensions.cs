using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcExportOrder.Extensions
{
    public static class OrderItemExtensions
    {
        public static ProductAttributeValue GetWarranty(this OrderItem orderItem)
        {
            var productAttributeParser = EngineContext.Current.Resolve<IProductAttributeParser>();
            var productAttributeService = EngineContext.Current.Resolve<IProductAttributeService>();
            return  productAttributeParser.ParseProductAttributeValues(
                        orderItem.AttributesXml
                    )
                    .Where(val => IsWarranty(
                        productAttributeService.GetProductAttributeById(
                            productAttributeService.GetProductAttributeMappingById(
                                val.ProductAttributeMappingId
                            ).ProductAttributeId
                        )))
                    .FirstOrDefault();
        }

        private static bool IsWarranty(ProductAttribute productAttribute)
        {
            return productAttribute.Name == "Warranty";
        }
    }
}
