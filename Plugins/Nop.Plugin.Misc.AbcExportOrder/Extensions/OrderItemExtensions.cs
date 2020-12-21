using System.Collections.Generic;
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

        public static bool IsPickup(this OrderItem oi)
        {
            return oi.AttributeDescription != null &&
                   oi.AttributeDescription.Contains("Pickup: ");
        }

        public static bool IsHomeDelivery(this OrderItem oi)
        {
            return oi.AttributeDescription != null &&
                   oi.AttributeDescription.Contains("Home Delivery: ");
        }

        public static bool HasWarranty(this OrderItem oi)
        {
            return oi.AttributeDescription != null &&
                   oi.AttributeDescription.Contains("Warranty: ");
        }

        public static (List<OrderItem> pickupItems, List<OrderItem> shippingItems) SplitByPickupAndShipping(this IList<OrderItem> ois)
        {
            var pickupItems = ois.Where(oi => oi.IsPickup());
            var shippingItems = ois.Except(pickupItems);

            return (pickupItems.ToList(), shippingItems.ToList());
        }

        private static bool IsWarranty(ProductAttribute productAttribute)
        {
            return productAttribute.Name == "Warranty";
        }
    }
}
