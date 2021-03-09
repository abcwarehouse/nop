using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Services.Shipping;

namespace Nop.Plugin.Misc.AbcCore.HomeDelivery
{
    public interface IHomeDeliveryCostService
    {
        decimal GetHomeDeliveryCost(IList<OrderItem> orderItems);
        decimal GetHomeDeliveryCost(IList<GetShippingOptionRequest.PackageItem> packageItems);
    }
}
