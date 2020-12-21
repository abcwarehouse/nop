using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.AbcExportOrder.Extensions;

namespace Nop.Plugin.Misc.AbcExportOrder.Models
{
    public class YahooDetailRow
    {
        public string Id { get; protected set; }
        public int LineNumber { get; protected set; }
        public string ItemId { get; protected set; }
        public string Code { get; protected set; }
        public int Quantity { get; protected set; }
        public decimal UnitPrice { get; protected set; }
        public string Description { get; protected set; }
        public string Url { get; protected set; }
        public string PickupBranchId { get; protected set; }

        public YahooDetailRow(
            string prefix,
            OrderItem orderItem,
            int itemLine,
            string itemId,
            string itemCode,
            string description,
            string url,
            string pickupBranchId
        )
        {
            Id = $"{prefix}{orderItem.OrderId}+{(orderItem.IsPickup() ? 'p' : 's')}";
            LineNumber = itemLine;
            ItemId = itemId;
            Code = itemCode;
            Quantity = orderItem.Quantity;
            UnitPrice = orderItem.UnitPriceExclTax;
            Description = description;
            Url = url;
            PickupBranchId = pickupBranchId;
        }
    }
}