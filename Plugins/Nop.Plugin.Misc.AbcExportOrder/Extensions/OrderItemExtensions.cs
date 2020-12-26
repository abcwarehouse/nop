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
        public static bool IsPickup(this OrderItem oi)
        {
            return !string.IsNullOrWhiteSpace(oi.AttributeDescription) &&
                   oi.AttributeDescription.Contains("Pickup: ");
        }

        public static bool IsHomeDelivery(this OrderItem oi)
        {
            return !string.IsNullOrWhiteSpace(oi.AttributeDescription) &&
                   oi.AttributeDescription.Contains("Home Delivery: ");
        }

        public static bool HasWarranty(this OrderItem oi)
        {
            return !string.IsNullOrWhiteSpace(oi.AttributeDescription) &&
                   oi.AttributeDescription.Contains("Warranty: ");
        }

        public static string GetMattressSize(this OrderItem oi)
        {
            if (oi.AttributeDescription == null || !oi.AttributeDescription.Contains("Mattress Size:"))
            {
                return null;
            }
            var mattressSizeIndex = oi.AttributeDescription.IndexOf("Mattress Size:");
            var mattressSizeString = oi.AttributeDescription.Substring(mattressSizeIndex);
            mattressSizeString = mattressSizeString.Substring(14, mattressSizeString.IndexOf("<br />") - 14);
            mattressSizeString = mattressSizeString.Substring(0, mattressSizeString.IndexOf("["));

            return mattressSizeString.Trim();
        }

        public static string GetBase(this OrderItem oi)
        {
            if (oi.AttributeDescription == null || !oi.AttributeDescription.Contains("Base ("))
            {
                return null;
            }
            var baseIndex = oi.AttributeDescription.IndexOf("Base (");
            var baseString = oi.AttributeDescription.Substring(baseIndex);
            baseString = baseString.Substring(6, baseString.IndexOf("<br />") - 6);
            baseString = baseString.Substring(0, baseString.IndexOf("["));
            baseString = baseString.Substring(baseString.IndexOf(":") + 1);

            return baseString.Trim();
        }

        public static (List<OrderItem> pickupItems, List<OrderItem> shippingItems) SplitByPickupAndShipping(this IList<OrderItem> ois)
        {
            var pickupItems = ois.Where(oi => oi.IsPickup());
            var shippingItems = ois.Except(pickupItems);

            return (pickupItems.ToList(), shippingItems.ToList());
        }
    }
}
