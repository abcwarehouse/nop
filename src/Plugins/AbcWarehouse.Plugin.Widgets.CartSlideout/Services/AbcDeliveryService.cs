using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Domain;
using Nop.Data;
using Nop.Services.Logging;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Services
{
    public class AbcDeliveryService : IAbcDeliveryService
    {
        private readonly ILogger _logger;
        private readonly IRepository<AbcDeliveryItem> _abcDeliveryItemRepository;
        private readonly IRepository<AbcDeliveryMap> _abcDeliveryMapRepository;

        public AbcDeliveryService(
            ILogger logger,
            IRepository<AbcDeliveryItem> abcDeliveryItemRepository,
            IRepository<AbcDeliveryMap> abcDeliveryMapRepository)
        {
            _logger = logger;
            _abcDeliveryItemRepository = abcDeliveryItemRepository;
            _abcDeliveryMapRepository = abcDeliveryMapRepository;
        }

        public async Task<AbcDeliveryItem> GetAbcDeliveryItemByItemNumberAsync(int itemNumber)
        {
            try
            {
                return await _abcDeliveryItemRepository.Table.SingleAsync(adi => adi.Item_Number == itemNumber);
            }
            catch (AggregateException)
            {
                await _logger.ErrorAsync($"Cannot find single AbcDeliveryItem with ItemNumber {itemNumber}");
                throw;
            }
        }

        public async Task<IList<AbcDeliveryMap>> GetAbcDeliveryMapsAsync()
        {
            return await _abcDeliveryMapRepository.Table.ToListAsync();
        }
    }
}
