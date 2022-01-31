using System.Collections.Generic;
using System.Threading.Tasks;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Domain;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Services
{
    public interface IAbcDeliveryService
    {
        Task<AbcDeliveryItem> GetAbcDeliveryItemByItemNumberAsync(int itemNumber);

        Task<IList<AbcDeliveryMap>> GetAbcDeliveryMapsAsync();
    }
}
