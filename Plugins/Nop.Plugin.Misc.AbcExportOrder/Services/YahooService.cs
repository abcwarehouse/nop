using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using System.Collections.Generic;
using Nop.Services.Common;
using Nop.Services.Orders;
using System.Linq;
using Nop.Services.Directory;
using Nop.Plugin.Misc.AbcExportOrder.Extensions;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public class YahooService : IYahooService
    {
        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly IOrderService _orderService;
        private readonly IStateProvinceService _stateProvinceService;

        private readonly ExportOrderSettings _settings;

        public YahooService(
            IAddressService addressService,
            ICountryService countryService,
            IOrderService orderService,
            IStateProvinceService stateProvinceService,
            ExportOrderSettings settings
        )
        {
            _addressService = addressService;
            _countryService = countryService;
            _orderService = orderService;
            _stateProvinceService = stateProvinceService;
            _settings = settings;
        }

        public IList<YahooHeaderRow> GetYahooHeaderRows(Order order)
        {
            throw new System.NotImplementedException();
        }

        public IList<YahooShipToRow> GetYahooShipToRows(
            Order order
        )
        {
            var result = new List<YahooShipToRow>();

            var orderItems = _orderService.GetOrderItems(order.Id);
            if (!orderItems.Any()) { return result; }
            
            var pickupItems = orderItems.Where(oi => oi.IsPickup());
            var shippingItems = orderItems.Except(pickupItems);

            if (pickupItems.Any())
            {
                result.Add(new YahooShipToRowPickup(
                    _settings.OrderIdPrefix, order.Id
                ));
            }

            if (shippingItems.Any())
            {
                var address = _addressService.GetAddressById(order.ShippingAddressId.Value);
                var stateAbbv = _stateProvinceService.GetStateProvinceByAddress(address).Abbreviation;
                var country = _countryService.GetCountryByAddress(address).Name;
                result.Add(new YahooShipToRowShipping(
                    _settings.OrderIdPrefix, order.Id, address, stateAbbv, country
                ));
            }
            
            return result;
        }
    }
}
