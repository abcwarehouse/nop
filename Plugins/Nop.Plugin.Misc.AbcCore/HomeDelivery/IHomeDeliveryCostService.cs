using Nop.Core.Domain.Orders;
using Nop.Services.Shipping;

namespace Nop.Plugin.Misc.AbcCore.HomeDelivery
{
    public interface IHomeDeliveryCostService
    {
        decimal GetHomeDeliveryCost(OrderItem orderItem);
        decimal GetHomeDeliveryCost(GetShippingOptionRequest.PackageItem packageItem);
    }
}
