using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Domain;
using Nop.Data;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Services
{
    public class AbcDeliveryService : IAbcDeliveryService
    {
        private readonly IRepository<AbcDeliveryItem> _abcDeliveryItemRepository;
        private readonly IRepository<AbcDeliveryMap> _abcDeliveryMapRepository;

        public AbcDeliveryService(
            IRepository<AbcDeliveryItem> abcDeliveryItemRepository,
            IRepository<AbcDeliveryMap> abcDeliveryMapRepository)
        {
            _abcDeliveryItemRepository = abcDeliveryItemRepository;
            _abcDeliveryMapRepository = abcDeliveryMapRepository;
        }

        public async Task<AbcDeliveryItem> GetAbcDeliveryItemByItemNumberAsync(int itemNumber)
        {
            return await _abcDeliveryItemRepository.Table.SingleAsync(adi => adi.Item_Number == itemNumber);
        }

        public async Task<IList<AbcDeliveryMap>> GetAbcDeliveryMapsAsync()
        {
            return await _abcDeliveryMapRepository.Table.ToListAsync();
        }
    }
}
