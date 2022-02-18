using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Misc.AbcCore.Delivery
{
    public interface IAbcDeliveryService
    {
        Task<AbcDeliveryItem> GetAbcDeliveryItemByItemNumberAsync(int itemNumber);

        Task<AbcDeliveryMap> GetAbcDeliveryMapByCategoryIdAsync(int categoryId);

        Task UpdateProductDeliveryOptionsAsync(
            Product product,
            bool AllowInStorePickup
        );
    }
}
