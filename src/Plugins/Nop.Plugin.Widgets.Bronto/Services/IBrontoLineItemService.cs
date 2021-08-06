using System.Threading.Tasks;
using Nop.Plugin.Widgets.Bronto.Domain;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Widgets.Bronto.Services
{
    public interface IBrontoLineItemService
    {
        Task<IList<BrontoLineItem>> GetBrontoLineItemsAsync(IList<ShoppingCartItem> cart);
        Task<IList<BrontoLineItem>> GetBrontoLineItemsAsync(IList<OrderItem> orderItems);
    }
}
