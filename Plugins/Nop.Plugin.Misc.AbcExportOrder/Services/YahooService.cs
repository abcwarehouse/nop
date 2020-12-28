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
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Stores;
using Nop.Services.Seo;
using System.Xml.Linq;
using Nop.Plugin.Misc.AbcFrontend.Services;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcMattresses.Services;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public class YahooService : IYahooService
    {
        private readonly IAbcMattressBaseService _abcMattressBaseService;
        private readonly IAbcMattressEntryService _abcMattressEntryService;
        private readonly IAbcMattressGiftService _abcMattressGiftService;
        private readonly IAbcMattressModelService _abcMattressModelService;
        private readonly IAbcMattressPackageService _abcMattressPackageService;
        private readonly IAddressService _addressService;
        private readonly IAttributeUtilities _attributeUtilities;
        private readonly ICountryService _countryService;
        private readonly ICustomOrderService _customOrderService;
        private readonly ICustomShopService _customShopService;
        private readonly IEncryptionService _encryptionService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGiftCardService _giftCardService;
        private readonly IProductService _productService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreService _storeService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWarrantyService _warrantyService;

        private readonly ExportOrderSettings _settings;
        private readonly SecuritySettings _securitySettings;


        public YahooService(
            IAbcMattressBaseService abcMattressBaseService,
            IAbcMattressEntryService abcMattressEntryService,
            IAbcMattressGiftService abcMattressGiftService,
            IAbcMattressModelService abcMattressModelService,
            IAbcMattressPackageService abcMattressPackageService,
            IAddressService addressService,
            IAttributeUtilities attributeUtilities,
            ICountryService countryService,
            ICustomOrderService customOrderService,
            ICustomShopService customShopService,
            IEncryptionService encryptionService,
            IGenericAttributeService genericAttributeService,
            IGiftCardService giftCardService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IProductAbcDescriptionService productAbcDescriptionService,
            IStateProvinceService stateProvinceService,
            IStoreService storeService,
            IUrlRecordService urlRecordService,
            IWarrantyService warrantyService,
            ExportOrderSettings settings,
            SecuritySettings securitySettings
        )
        {
            _abcMattressBaseService = abcMattressBaseService;
            _abcMattressEntryService = abcMattressEntryService;
            _abcMattressGiftService = abcMattressGiftService;
            _abcMattressModelService = abcMattressModelService;
            _abcMattressPackageService = abcMattressPackageService;
            _addressService = addressService;
            _attributeUtilities = attributeUtilities;
            _countryService = countryService;
            _customOrderService = customOrderService;
            _customShopService = customShopService;
            _encryptionService = encryptionService;
            _genericAttributeService = genericAttributeService;
            _giftCardService = giftCardService;
            _productService = productService;
            _productAbcDescriptionService = productAbcDescriptionService;
            _priceCalculationService = priceCalculationService;
            _stateProvinceService = stateProvinceService;
            _storeService = storeService;
            _urlRecordService = urlRecordService;
            _warrantyService = warrantyService;
            _settings = settings;
            _securitySettings = securitySettings;
        }

        public IList<YahooDetailRow> GetYahooDetailRows(Order order)
        {
            var result = new List<YahooDetailRow>();
            var pickupLineNumber = 0;
            var shippingLineNumber = 0;
            var orderItems = _customOrderService.GetOrderItems(order.Id);

            foreach (var orderItem in orderItems)
            {
                int lineNumber = GetLineNumber(ref pickupLineNumber, ref shippingLineNumber, orderItem);

                var product = _productService.GetProductById(orderItem.ProductId);
                var productAbcDescription = _productAbcDescriptionService.GetProductAbcDescriptionByProductId(
                    orderItem.ProductId
                );
                var storeUrl = _storeService.GetStoreById(order.StoreId)?.Url;
                string standardItemCode = GetCode(orderItem, product, productAbcDescription);

                var warranty = _customOrderService.GetOrderItemWarranty(orderItem);
                if (warranty != null)
                {
                    // adjust price for item
                    orderItem.UnitPriceExclTax -= warranty.PriceAdjustment;
                }

                result.Add(new YahooDetailRow(
                    _settings.OrderIdPrefix,
                    orderItem,
                    lineNumber,
                    product.Sku,
                    standardItemCode,
                    product.Name,
                    $"{storeUrl}{_urlRecordService.GetSeName(product)}",
                    GetPickupStore(orderItem)
                ));

                if (warranty != null)
                {
                    lineNumber++;
                    result.Add(new YahooDetailRow(
                        _settings.OrderIdPrefix,
                        orderItem,
                        lineNumber,
                        standardItemCode,
                        _warrantyService.GetWarrantySkuByName(warranty.Name),
                        warranty.PriceAdjustment,
                        warranty.Name,
                        "", // no url for warranty line items
                        GetPickupStore(orderItem)
                    ));
                }

                var freeGift = orderItem.GetFreeGift();
                if (freeGift != null)
                {
                    lineNumber++;
                    var amg = _abcMattressGiftService.GetAbcMattressGiftByDescription(freeGift);
                    result.Add(new YahooDetailRow(
                        _settings.OrderIdPrefix,
                        orderItem,
                        lineNumber,
                        "", // no item ID associated
                        amg.ItemNo,
                        0.00M, // free item
                        freeGift,
                        "", // no url for free gifts
                        GetPickupStore(orderItem)
                    ));
                }

                SetLineNumber(ref pickupLineNumber, ref shippingLineNumber, orderItem, lineNumber);
            }

            return result;
        }

        private string GetCode(OrderItem orderItem, Product product, ProductAbcDescription productAbcDescription)
        {
            var mattressSize = orderItem.GetMattressSize();
            if (mattressSize != null)
            {
                var model = _abcMattressModelService.GetAbcMattressModelByProductId(product.Id);
                var entry = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id)
                                                    .Where(e => e.Size == mattressSize)
                                                    .FirstOrDefault();

                var baseName = orderItem.GetBase();
                if (baseName != null)
                {
                    var abcMattressBase = _abcMattressBaseService.GetAbcMattressBasesByEntryId(entry.Id)
                                                                 .Where(b => b.Name == baseName)
                                                                 .FirstOrDefault();
                    var package = _abcMattressPackageService.GetAbcMattressPackagesByEntryIds(new int[]{ entry.Id })
                                                            .Where(p => p.AbcMattressBaseId == abcMattressBase.Id)
                                                            .FirstOrDefault();

                    return package.ItemNo;
                }

                
                return entry.ItemNo;
            }

            return productAbcDescription != null ? productAbcDescription.AbcItemNumber : product.Sku;
        }

        private static void SetLineNumber(
            ref int pickupLineNumber,
            ref int shippingLineNumber,
            OrderItem orderItem,
            int lineNumber
        )
        {
            if (orderItem.IsPickup())
            {
                pickupLineNumber = lineNumber;
            }
            else
            {
                shippingLineNumber = lineNumber;
            }
        }

        private static int GetLineNumber(
            ref int pickupLineNumber,
            ref int shippingLineNumber,
            OrderItem orderItem
        )
        {
            var lineNumber = 0;
            if (orderItem.IsPickup())
            {
                pickupLineNumber++;
                lineNumber = pickupLineNumber;
            }
            else
            {
                shippingLineNumber++;
                lineNumber = shippingLineNumber;
            }

            return lineNumber;
        }

        public IList<YahooHeaderRow> GetYahooHeaderRows(Order order)
        {
            var result = new List<YahooHeaderRow>();

            var orderItems = _customOrderService.GetOrderItems(order.Id);
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
            

            var ccRefNo = _genericAttributeService.GetAttribute<string>(order, "CardRefNo");

            var pickupItems = splitItems.pickupItems;
            if (pickupItems.Any())
            {
                decimal backendOrderTax, backendOrderTotal;
                CalculateValues(pickupItems, out backendOrderTax, out backendOrderTotal);

                string giftCardCode;
                decimal giftCardUsed;
                CalculateGiftCard(order, backendOrderTotal, out giftCardCode, out giftCardUsed);
                
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

            var shippingItems = splitItems.shippingItems;
            if (shippingItems.Any())
            {
                decimal homeDeliveryCost = 0;
                decimal shippingCost = 0;
                decimal homeDeliveryCostPerItem = 14.75M;

                homeDeliveryCost = 0;
                foreach (OrderItem item in shippingItems)
                {
                    if (item.IsHomeDelivery())
                    {
                        homeDeliveryCost += homeDeliveryCostPerItem * item.Quantity;
                    }
                }
                shippingCost = order.OrderShippingExclTax - homeDeliveryCost;

                decimal backendOrderTax, backendOrderTotal;
                CalculateValues(shippingItems, out backendOrderTax, out backendOrderTotal);

                decimal shippingTax = order.OrderShippingInclTax - order.OrderShippingExclTax;
                backendOrderTax += shippingTax;
                backendOrderTotal += order.OrderShippingInclTax;

                string giftCardCode;
                decimal giftCardUsed;
                CalculateGiftCard(order, backendOrderTotal, out giftCardCode, out giftCardUsed);

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

            var orderItems = _customOrderService.GetOrderItems(order.Id);
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

        private void CalculateGiftCard(Order order, decimal backendOrderTotal, out string giftCardCode, out decimal giftCardUsed)
        {
            giftCardCode = "";
            giftCardUsed = 0;
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
        }

        private static void CalculateValues(IList<OrderItem> orderItems, out decimal backendOrderTax, out decimal backendOrderTotal)
        {
            backendOrderTax = 0;
            backendOrderTotal = 0;
            foreach (OrderItem item in orderItems)
            {
                backendOrderTax += item.PriceInclTax - item.PriceExclTax;
                backendOrderTotal += item.PriceInclTax;
            }
        }

        private string GetPickupStore(OrderItem orderItem)
        {
            var pickupInStoreAttributeMapping = _attributeUtilities.GetPickupAttributeMapping(orderItem.AttributesXml);
            if (pickupInStoreAttributeMapping == null) return "";

            var xmlDoc = XDocument.Parse(orderItem.AttributesXml);

            var pickupInStoreAttribute = xmlDoc.Descendants()
                                               .Where(d => d.Name == "ProductAttribute" &&
                                                           d.FirstAttribute != null &&
                                                           d.FirstAttribute.Value == pickupInStoreAttributeMapping.Id.ToString())
                                               .FirstOrDefault();
            if (pickupInStoreAttribute == null) return "";

            var storeName = pickupInStoreAttribute.Value;
            storeName = storeName.Remove(storeName.IndexOf("\n"));

            var shop = _customShopService.GetShopByName(storeName);
            if (shop == null) return "";

            var shopAbc = _customShopService.GetShopAbcByShopId(shop.Id);
            if (shopAbc == null) return "";

            return shopAbc.AbcId.ToString();
        }
    }
}
