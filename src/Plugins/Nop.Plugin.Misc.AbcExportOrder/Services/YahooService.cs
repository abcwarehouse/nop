using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using System.Collections.Generic;
using Nop.Services.Common;
using Nop.Services.Orders;
using System.Linq;
using Nop.Services.Directory;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Core.Domain.Security;
using Nop.Services.Security;
using Nop.Services.Catalog;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Stores;
using Nop.Services.Seo;
using System.Xml.Linq;
using Nop.Plugin.Misc.AbcFrontend.Services;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.HomeDelivery;
using Nop.Services.Payments;
using System;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public class YahooService : IYahooService
    {
        private readonly IAbcMattressBaseService _abcMattressBaseService;
        private readonly IAbcMattressEntryService _abcMattressEntryService;
        private readonly IAbcMattressFrameService _abcMattressFrameService;
        private readonly IAbcMattressGiftService _abcMattressGiftService;
        private readonly IAbcMattressModelService _abcMattressModelService;
        private readonly IAbcMattressPackageService _abcMattressPackageService;
        private readonly IAbcMattressProtectorService _abcMattressProtectorService;
        private readonly IAddressService _addressService;
        private readonly IAttributeUtilities _attributeUtilities;
        private readonly ICountryService _countryService;
        private readonly ICustomOrderService _customOrderService;
        private readonly ICustomShopService _customShopService;
        private readonly IEncryptionService _encryptionService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGiftCardService _giftCardService;
        private readonly IHomeDeliveryCostService _homeDeliveryCostService;
        private readonly IProductService _productService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreService _storeService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWarrantyService _warrantyService;

        private readonly ExportOrderSettings _settings;
        private readonly SecuritySettings _securitySettings;
        private readonly IPaymentService _paymentService;


        public YahooService(
            IAbcMattressBaseService abcMattressBaseService,
            IAbcMattressEntryService abcMattressEntryService,
            IAbcMattressFrameService abcMattressFrameService,
            IAbcMattressGiftService abcMattressGiftService,
            IAbcMattressModelService abcMattressModelService,
            IAbcMattressPackageService abcMattressPackageService,
            IAbcMattressProtectorService abcMattressProtectorService,
            IAddressService addressService,
            IAttributeUtilities attributeUtilities,
            ICountryService countryService,
            ICustomOrderService customOrderService,
            ICustomShopService customShopService,
            IEncryptionService encryptionService,
            IGenericAttributeService genericAttributeService,
            IGiftCardService giftCardService,
            IHomeDeliveryCostService homeDeliveryCostService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IProductAbcDescriptionService productAbcDescriptionService,
            IStateProvinceService stateProvinceService,
            IStoreService storeService,
            IUrlRecordService urlRecordService,
            IWarrantyService warrantyService,
            ExportOrderSettings settings,
            SecuritySettings securitySettings,
            IPaymentService paymentService
        )
        {
            _abcMattressBaseService = abcMattressBaseService;
            _abcMattressEntryService = abcMattressEntryService;
            _abcMattressFrameService = abcMattressFrameService;
            _abcMattressGiftService = abcMattressGiftService;
            _abcMattressModelService = abcMattressModelService;
            _abcMattressPackageService = abcMattressPackageService;
            _abcMattressProtectorService = abcMattressProtectorService;
            _addressService = addressService;
            _attributeUtilities = attributeUtilities;
            _countryService = countryService;
            _customOrderService = customOrderService;
            _customShopService = customShopService;
            _encryptionService = encryptionService;
            _genericAttributeService = genericAttributeService;
            _giftCardService = giftCardService;
            _homeDeliveryCostService = homeDeliveryCostService;
            _productService = productService;
            _productAbcDescriptionService = productAbcDescriptionService;
            _priceCalculationService = priceCalculationService;
            _stateProvinceService = stateProvinceService;
            _storeService = storeService;
            _urlRecordService = urlRecordService;
            _warrantyService = warrantyService;
            _settings = settings;
            _securitySettings = securitySettings;
            _paymentService = paymentService;
        }

        public async Task<IList<YahooDetailRow>> GetYahooDetailRowsAsync(Order order)
        {
            var result = new List<YahooDetailRow>();
            var pickupLineNumber = 0;
            var shippingLineNumber = 0;
            var orderItems = await _customOrderService.GetOrderItemsAsync(order.Id);

            foreach (var orderItem in orderItems)
            {
                int lineNumber = GetLineNumber(ref pickupLineNumber, ref shippingLineNumber, orderItem);

                var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
                var productAbcDescription = await _productAbcDescriptionService.GetProductAbcDescriptionByProductIdAsync(
                    orderItem.ProductId
                );
                var storeUrl = (await _storeService.GetStoreByIdAsync(order.StoreId))?.Url;
                var warranty = await _customOrderService.GetOrderItemWarrantyAsync(orderItem);
                if (warranty != null)
                {
                    // adjust price for item
                    orderItem.UnitPriceExclTax -= warranty.PriceAdjustment;
                }

                (string code, decimal price) standardItemCodeAndPrice =
                    GetCodeAndPrice(orderItem, product, productAbcDescription);

                result.Add(new YahooDetailRow(
                    _settings.OrderIdPrefix,
                    orderItem,
                    lineNumber,
                    product.Sku,
                    standardItemCodeAndPrice.code,
                    standardItemCodeAndPrice.price,
                    product.Name,
                    $"{storeUrl}{await _urlRecordService.GetSeNameAsync(product)}",
                    await GetPickupStoreAsync(orderItem)
                ));

                if (warranty != null)
                {
                    lineNumber++;
                    result.Add(new YahooDetailRow(
                        _settings.OrderIdPrefix,
                        orderItem,
                        lineNumber,
                        standardItemCodeAndPrice.code,
                        _warrantyService.GetWarrantySkuByName(warranty.Name),
                        warranty.PriceAdjustment,
                        warranty.Name,
                        "", // no url for warranty line items
                        await GetPickupStoreAsync(orderItem)
                    ));
                }

                var freeGift = orderItem.GetFreeGift();
                if (freeGift != null)
                {
                    lineNumber++;
                    var amg = _abcMattressGiftService.GetAbcMattressGiftByDescription(freeGift);
                    if (amg == null)
                    {
                        throw new Exception($"Unable to match free gift named {freeGift}");
                    }

                    result.Add(new YahooDetailRow(
                        _settings.OrderIdPrefix,
                        orderItem,
                        lineNumber,
                        "", // no item ID associated
                        amg.ItemNo,
                        0.00M, // free item
                        freeGift,
                        "", // no url for free gifts
                        await GetPickupStoreAsync(orderItem),
                        -1
                    ));
                }

                var mattressProtector = orderItem.GetMattressProtector();
                if (mattressProtector != null)
                {
                    lineNumber++;
                    var size = orderItem.GetMattressSize();
                    var amp = _abcMattressProtectorService.GetAbcMattressProtectorsBySize(size)
                                                          .Where(p => p.Name == mattressProtector)
                                                          .FirstOrDefault();
                    if (amp == null)
                    {
                        throw new Exception($"Unable to match mattress protector named {mattressProtector}");
                    }

                    result.Add(new YahooDetailRow(
                        _settings.OrderIdPrefix,
                        orderItem,
                        lineNumber,
                        "", // no item ID associated
                        amp.ItemNo,
                        amp.Price,
                        mattressProtector,
                        "", // no url
                        await GetPickupStoreAsync(orderItem)
                    ));
                }

                await ProcessFrameAsync(orderItem, lineNumber, result);

                SetLineNumber(ref pickupLineNumber, ref shippingLineNumber, orderItem, lineNumber);
            }

            return result;
        }

        private async Task ProcessFrameAsync(OrderItem orderItem, int lineNumber, List<YahooDetailRow> result)
        {
            var frame = orderItem.GetFrame();
            if (frame != null)
            {
                lineNumber++;
                var size = orderItem.GetMattressSize();
                var amf = _abcMattressFrameService.GetAbcMattressFramesBySize(size)
                                                      .Where(p => p.Name == frame)
                                                      .FirstOrDefault();
                result.Add(new YahooDetailRow(
                    _settings.OrderIdPrefix,
                    orderItem,
                    lineNumber,
                    "", // no item ID associated
                    amf.ItemNo,
                    amf.Price,
                    frame,
                    "", // no url
                    await GetPickupStoreAsync(orderItem)
                ));
            }
        }

        private (string, decimal) GetCodeAndPrice(
            OrderItem orderItem,
            Product product,
            ProductAbcDescription productAbcDescription
        )
        {
            var mattressSize = orderItem.GetMattressSize();
            if (mattressSize != null)
            {
                var model = _abcMattressModelService.GetAbcMattressModelByProductId(product.Id);
                if (model == null) { throw new Exception($"Unable to find model for productId {product.Id}, size {mattressSize}"); }

                var entry = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id)
                                                    .Where(e => e.Size == mattressSize)
                                                    .FirstOrDefault();
                if (entry == null) { throw new Exception($"Unable to find entry for model {model.Name}, size {mattressSize}"); }

                var baseName = orderItem.GetBase();
                if (baseName != null)
                {
                    var abcMattressBase = _abcMattressBaseService.GetAbcMattressBasesByEntryId(entry.Id)
                                                                 .Where(b => b.Name == baseName)
                                                                 .FirstOrDefault();
                    if (abcMattressBase == null)
                    {
                        throw new Exception($"Unable to find base for model {model.Name}, size {entry.Size}, base {baseName}");
                    }
                    var package = _abcMattressPackageService.GetAbcMattressPackagesByEntryIds(new int[] { entry.Id })
                                                            .Where(p => p.AbcMattressBaseId == abcMattressBase.Id)
                                                            .FirstOrDefault();
                    if (package == null)
                    {
                        throw new Exception($"Unable to find base for model {model.Name}, size {entry.Size}, base {baseName}");
                    }

                    return (package.ItemNo, package.Price);
                }


                return (entry.ItemNo, entry.Price);
            }

            return (productAbcDescription != null ?
                        productAbcDescription.AbcItemNumber : product.Sku,
                    orderItem.UnitPriceExclTax);
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

        public async Task<IList<YahooHeaderRow>> GetYahooHeaderRowsAsync(Order order)
        {
            var result = new List<YahooHeaderRow>();

            var orderItems = await _customOrderService.GetOrderItemsAsync(order.Id);
            if (!orderItems.Any()) { return result; }

            var splitItems = orderItems.SplitByPickupAndShipping();
            var address = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
            var stateAbbv = (await _stateProvinceService.GetStateProvinceByAddressAsync(address)).Abbreviation;
            var country = (await _countryService.GetCountryByAddressAsync(address)).Name;
            var cardName = _encryptionService.DecryptText(order.CardName, _securitySettings.EncryptionKey);
            var cardNumber = _encryptionService.DecryptText(order.CardNumber, _securitySettings.EncryptionKey);
            var cardMonth = _encryptionService.DecryptText(order.CardExpirationMonth, _securitySettings.EncryptionKey);
            var cardYear = _encryptionService.DecryptText(order.CardExpirationYear, _securitySettings.EncryptionKey);
            var cardCvv2 = _encryptionService.DecryptText(order.CardCvv2, _securitySettings.EncryptionKey);
            var customValues = _paymentService.DeserializeCustomValues(order);
            var ccRefNo = customValues != null ?
                customValues.Where(cv => cv.Key == "CC_REFNO")
                            .Select(cv => cv.Value?.ToString())
                            .FirstOrDefault() :
                null;

            var pickupItems = splitItems.pickupItems;
            if (pickupItems.Any())
            {
                decimal backendOrderTax, backendOrderTotal;
                CalculateValues(pickupItems, out backendOrderTax, out backendOrderTotal);

                var giftCard = await CalculateGiftCardAsync(order, backendOrderTotal);

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
                    await _priceCalculationService.RoundPriceAsync(backendOrderTax),
                    await _priceCalculationService.RoundPriceAsync(backendOrderTotal),
                    giftCard.GiftCardCode,
                    giftCard.GiftCardUsage,
                    ccRefNo
                ));
            }

            var shippingItems = splitItems.shippingItems;
            if (shippingItems.Any())
            {
                decimal homeDeliveryCost = 0;
                decimal shippingCost = 0;

                homeDeliveryCost = await _homeDeliveryCostService.GetHomeDeliveryCostAsync(shippingItems);
                shippingCost = order.OrderShippingExclTax - homeDeliveryCost;

                decimal backendOrderTax, backendOrderTotal;
                CalculateValues(shippingItems, out backendOrderTax, out backendOrderTotal);

                decimal shippingTax = order.OrderShippingInclTax - order.OrderShippingExclTax;
                backendOrderTax += shippingTax;
                backendOrderTotal += order.OrderShippingInclTax;

                var giftCard = await CalculateGiftCardAsync(order, backendOrderTotal);

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
                    await _priceCalculationService.RoundPriceAsync(backendOrderTax),
                    await _priceCalculationService.RoundPriceAsync(shippingCost),
                    await _priceCalculationService.RoundPriceAsync(homeDeliveryCost),
                    await _priceCalculationService.RoundPriceAsync(backendOrderTotal),
                    giftCard.GiftCardCode,
                    giftCard.GiftCardUsage,
                    ccRefNo
                ));
            }

            return result;
        }

        public async Task<IList<YahooShipToRow>> GetYahooShipToRowsAsync(
            Order order
        )
        {
            var result = new List<YahooShipToRow>();

            var orderItems = await _customOrderService.GetOrderItemsAsync(order.Id);
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
                var address = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
                var stateAbbv = (await _stateProvinceService.GetStateProvinceByAddressAsync(address)).Abbreviation;
                var country = (await _countryService.GetCountryByAddressAsync(address)).Name;
                result.Add(new YahooShipToRowShipping(
                    _settings.OrderIdPrefix, order.Id, address, stateAbbv, country
                ));
            }

            return result;
        }

        private async Task<(string GiftCardCode, decimal GiftCardUsage)> CalculateGiftCardAsync(Order order, decimal backendOrderTotal)
        {
            var giftCardCode = "";
            decimal giftCardUsed = 0;
            var gcUsage = (await _giftCardService.GetGiftCardUsageHistoryAsync(order)).FirstOrDefault();
            if (gcUsage != null)
            {
                giftCardCode = (await _giftCardService.GetGiftCardByIdAsync(gcUsage.GiftCardId)).GiftCardCouponCode.Substring(3);
                if (gcUsage.UsedValue >= backendOrderTotal)
                {
                    giftCardUsed = backendOrderTotal;
                }
                else
                {
                    giftCardUsed = gcUsage.UsedValue;
                }
                giftCardUsed = await _priceCalculationService.RoundPriceAsync(giftCardUsed);
                gcUsage.UsedValue -= giftCardUsed;
            }

            return (giftCardCode, giftCardUsed);
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

        private async Task<string> GetPickupStoreAsync(OrderItem orderItem)
        {
            var pickupInStoreAttributeMapping = await _attributeUtilities.GetPickupAttributeMappingAsync(orderItem.AttributesXml);
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
