using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using System.Collections.Generic;
using Nop.Services.Common;
using Nop.Services.Orders;
using System.Linq;
using Nop.Services.Directory;
using Nop.Plugin.Misc.AbcExportOrder.Extensions;
using Nop.Core.Domain.Security;
using Nop.Services.Security;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public class YahooService : IYahooService
    {
        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly IEncryptionService _encryptionService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGiftCardService _giftCardService;
        private readonly IOrderService _orderService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IStateProvinceService _stateProvinceService;

        private readonly ExportOrderSettings _settings;
        private readonly SecuritySettings _securitySettings;

        public YahooService(
            IAddressService addressService,
            ICountryService countryService,
            IEncryptionService encryptionService,
            IGenericAttributeService genericAttributeService,
            IGiftCardService giftCardService,
            IOrderService orderService,
            IPriceCalculationService priceCalculationService,
            IStateProvinceService stateProvinceService,
            ExportOrderSettings settings,
            SecuritySettings securitySettings
        )
        {
            _addressService = addressService;
            _countryService = countryService;
            _encryptionService = encryptionService;
            _genericAttributeService = genericAttributeService;
            _giftCardService = giftCardService;
            _orderService = orderService;
            _priceCalculationService = priceCalculationService;
            _stateProvinceService = stateProvinceService;
            _settings = settings;
            _securitySettings = securitySettings;
        }

        public IList<YahooHeaderRow> GetYahooHeaderRows(Order order)
        {
            var result = new List<YahooHeaderRow>();

            var orderItems = _orderService.GetOrderItems(order.Id);
            if (!orderItems.Any()) { return result; }
            
            var splitItems = orderItems.SplitByPickupAndShipping();
            var address = _addressService.GetAddressById(order.BillingAddressId);
            var stateAbbv = _stateProvinceService.GetStateProvinceByAddress(address).Abbreviation;
            var country = _countryService.GetCountryByAddress(address).Name;
            var cardName = _encryptionService.DecryptText(order.CardName, _securitySettings.EncryptionKey);
            var cardNumber = _encryptionService.DecryptText(order.CardNumber, _securitySettings.EncryptionKey);
            var cardMonth = _encryptionService.DecryptText(order.CardExpirationMonth, _securitySettings.EncryptionKey);
            var cardYear = _encryptionService.DecryptText(order.CardExpirationYear, _securitySettings.EncryptionKey);
            var cardCvv2 = _encryptionService.DecryptText(order.CardCvv2, _securitySettings.EncryptionKey);

            decimal backendOrderTax = 0;
            decimal backendOrderTotal = 0;
            foreach (OrderItem item in orderItems)
            {
                backendOrderTax += item.PriceInclTax - item.PriceExclTax;
                backendOrderTotal += item.PriceInclTax;
            }

            string giftCardCode = "";
            decimal giftCardUsed = 0;
            var gcUsage = _giftCardService.GetGiftCardUsageHistory(order).FirstOrDefault();
            if (gcUsage != null)
            {
                giftCardCode = _giftCardService.GetGiftCardById(gcUsage.GiftCardId).GiftCardCouponCode.Substring(3);
                if (gcUsage.UsedValue >= backendOrderTotal)
                {
                    giftCardUsed = backendOrderTotal;
                }
                else
                {
                    giftCardUsed = gcUsage.UsedValue;
                }
                giftCardUsed = _priceCalculationService.RoundPrice(giftCardUsed);
                gcUsage.UsedValue -= giftCardUsed;
            }

            var ccRefNo = _genericAttributeService.GetAttribute<string>(order, "CardRefNo");

            if (splitItems.pickupItems.Any())
            {
                result.Add(new YahooHeaderRow(
                    _settings.OrderIdPrefix,
                    order,
                    address,
                    stateAbbv,
                    country,
                    cardName,
                    cardNumber,
                    cardMonth,
                    cardYear,
                    cardCvv2,
                    _priceCalculationService.RoundPrice(backendOrderTax),
                    _priceCalculationService.RoundPrice(backendOrderTotal),
                    giftCardCode,
                    giftCardUsed,
                    ccRefNo
                ));
            }

            if (splitItems.shippingItems.Any())
            {
                decimal homeDeliveryCost = 0;
                decimal shippingCost = 0;
                decimal homeDeliveryCostPerItem = 14.75M;

                homeDeliveryCost = 0;
                foreach (OrderItem item in orderItems)
                {
                    if (item.IsHomeDelivery())
                    {
                        homeDeliveryCost += homeDeliveryCostPerItem * item.Quantity;
                    }
                }
                shippingCost = order.OrderShippingExclTax - homeDeliveryCost;

                decimal shippingTax = order.OrderShippingInclTax - order.OrderShippingExclTax;
                backendOrderTax += shippingTax;
                backendOrderTotal += order.OrderShippingInclTax;

                result.Add(new YahooHeaderRowShipping(
                    _settings.OrderIdPrefix,
                    order,
                    address,
                    stateAbbv,
                    country,
                    cardName,
                    cardNumber,
                    cardMonth,
                    cardYear,
                    cardCvv2,
                    _priceCalculationService.RoundPrice(backendOrderTax),
                    _priceCalculationService.RoundPrice(shippingCost),
                    _priceCalculationService.RoundPrice(homeDeliveryCost),
                    _priceCalculationService.RoundPrice(backendOrderTotal),
                    giftCardCode,
                    giftCardUsed,
                    ccRefNo
                ));
            }
            
            return result;
        }

        public IList<YahooShipToRow> GetYahooShipToRows(
            Order order
        )
        {
            var result = new List<YahooShipToRow>();

            var orderItems = _orderService.GetOrderItems(order.Id);
            if (!orderItems.Any()) { return result; }
            
            var splitItems = orderItems.SplitByPickupAndShipping();

            if (splitItems.pickupItems.Any())
            {
                result.Add(new YahooShipToRow(
                    _settings.OrderIdPrefix, order.Id
                ));
            }

            if (splitItems.shippingItems.Any())
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
