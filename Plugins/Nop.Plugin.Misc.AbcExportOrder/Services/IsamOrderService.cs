using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Security;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using Nop.Services.Catalog;
using Nop.Core.Domain.Catalog;
using Nop.Core;
using Nop.Plugin.Misc.AbcExportOrder.Extensions;
using Nop.Services.Logging;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Seo;
using System.Xml.Linq;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public class IsamOrderService : IIsamOrderService
    {
        private readonly string HEADER_TABLE_NAME = "YAHOO_HEADER";
        private readonly string DETAIL_TABLE_NAME = "YAHOO_DETAIL";
        private readonly string SHIPTO_TABLE_NAME = "YAHOO_SHIPTO";
        private readonly char PICKUP_IN_STORE_FLAG = 'p';
        private readonly char SHIP_FLAG = 's';

        private List<string> _headerCols = new List<string>();
        private List<string> _detailCols = new List<string>();
        private List<string> _shiptoCols = new List<string>();

        private List<OdbcParameter> _headerParams = new List<OdbcParameter>();
        private List<OdbcParameter> _detailParams = new List<OdbcParameter>();
        private List<OdbcParameter> _shiptoParams = new List<OdbcParameter>();
        private HashSet<string> _canBeNullSet = new HashSet<string>{ "PICKUP_BRANCH", "PICKUP_DROP", "GIFT_CARD", "AUTH" };


        private ISettingService _settingService;
        private IEncryptionService _encryptionService;
        private IBaseService _baseIsamService;
        private IProductAttributeParser _productAttributeParser;
        private IAttributeUtilities _attributeUtilities;
        private IRepository<ShopAbc> _shopAbcRepository;
        private IRepository<CustomerShopMapping> _customerShopMappingRepository;
        private IRepository<WarrantySku> _warrantySkuRepository;
        private IRepository<ProductAbcDescription> _productAbcDescriptionRepository;
        private IRepository<GiftCardUsageHistory> _giftCardUsageHistoryRepository;
        private IWorkContext _workContext;
        private IStoreContext _storeContext;
        private IGiftCardService _giftCardService;
        private readonly ExportOrderSettings _settings;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IAddressService _addressService;
        private readonly IProductService _productService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IRepository<Product> _productRepository;
        private readonly ICustomShopService _customShopService;
        private readonly IIsamGiftCardService _isamGiftCardService;

        public IsamOrderService(
            ISettingService settingService, 
            IEncryptionService encryptionService, 
            IBaseService baseIsamService,
            IProductAttributeParser productAttributeParser,
            IAttributeUtilities attributeUtilities,
            IRepository<ShopAbc> shopAbcRepository,
            IRepository<CustomerShopMapping> customerShopMappingRepository,
            IRepository<WarrantySku> warrantySkuRepository,
            IRepository<ProductAbcDescription> productAbcDescriptionRepository,
            IRepository<GiftCardUsageHistory> giftCardUsageHistoryRepository,
            IStoreContext storeContext,
            IWorkContext workContext,
            IGiftCardService giftCardService,
            ExportOrderSettings settings,
            ILogger logger,
            IOrderService orderService,
            IPriceCalculationService priceCalculationService,
            IAddressService addressService,
            IProductService productService,
            IStateProvinceService stateProvinceService,
            ICountryService countryService,
            IUrlRecordService urlRecordService,
            IProductAttributeService productAttributeService,
            IRepository<Product> productRepository,
            ICustomShopService customShopService,
            IIsamGiftCardService isamGiftCardService
        )
        {
            _settingService = settingService;
            _encryptionService = encryptionService;
            _baseIsamService = baseIsamService;
            _productAttributeParser = productAttributeParser;
            _attributeUtilities = attributeUtilities;
            _shopAbcRepository = shopAbcRepository;
            _customerShopMappingRepository = customerShopMappingRepository;
            _warrantySkuRepository = warrantySkuRepository;
            _productAbcDescriptionRepository = productAbcDescriptionRepository;
            _giftCardUsageHistoryRepository = giftCardUsageHistoryRepository;
            _storeContext = storeContext;
            _workContext = workContext;
            _giftCardService = giftCardService;
            _settings = settings;
            _logger = logger;
            _orderService = orderService;
            _priceCalculationService = priceCalculationService;
            _addressService = addressService;
            _productService = productService;
            _stateProvinceService = stateProvinceService;
            _countryService = countryService;
            _urlRecordService = urlRecordService;
            _productAttributeService = productAttributeService;
            _productRepository = productRepository;
            _customShopService = customShopService;
            _isamGiftCardService = isamGiftCardService;

            InitializeAllColParams();
        }

        /// <summary>
        /// initializes parameters to  be ready for insert
        /// </summary>
        private void InitializeAllColParams()
        {
            _headerCols.Add("ID");
            _headerParams.Add(new OdbcParameter("@ID", OdbcType.VarChar));
            _headerCols.Add("DATESTAMP");
            _headerParams.Add(new OdbcParameter("@DATESTAMP", OdbcType.VarChar));
            _headerCols.Add("BILL_FULL");
            _headerParams.Add(new OdbcParameter("@BILL_FULL", OdbcType.VarChar));
            _headerCols.Add("BILL_FIRST");
            _headerParams.Add(new OdbcParameter("@BILL_FIRST", OdbcType.VarChar));
            _headerCols.Add("BILL_LAST");
            _headerParams.Add(new OdbcParameter("@BILL_LAST", OdbcType.VarChar));
            _headerCols.Add("BILL_ADDRESS_1");
            _headerParams.Add(new OdbcParameter("@BILL_ADDRESS_1", OdbcType.VarChar));
            _headerCols.Add("BILL_ADDRESS_2");
            _headerParams.Add(new OdbcParameter("@BILL_ADDRESS_2", OdbcType.VarChar));
            _headerCols.Add("BILL_CITY");
            _headerParams.Add(new OdbcParameter("@BILL_CITY", OdbcType.VarChar));
            _headerCols.Add("BILL_STATE");
            _headerParams.Add(new OdbcParameter("@BILL_STATE", OdbcType.VarChar));
            _headerCols.Add("BILL_ZIP");
            _headerParams.Add(new OdbcParameter("@BILL_ZIP", OdbcType.VarChar));
            _headerCols.Add("BILL_COUNTRY");
            _headerParams.Add(new OdbcParameter("@BILL_COUNTRY", OdbcType.VarChar));
            _headerCols.Add("BILL_PHONE");
            _headerParams.Add(new OdbcParameter("@BILL_PHONE", OdbcType.VarChar));
            _headerCols.Add("BILL_EMAIL");
            _headerParams.Add(new OdbcParameter("@BILL_EMAIL", OdbcType.VarChar));
            _headerCols.Add("CARD_NAME");
            _headerParams.Add(new OdbcParameter("@CARD_NAME", OdbcType.VarChar));
            _headerCols.Add("CARD_NUMBER");
            _headerParams.Add(new OdbcParameter("@CARD_NUMBER", OdbcType.VarChar));
            _headerCols.Add("CARD_EXPIRY");
            _headerParams.Add(new OdbcParameter("@CARD_EXPIRY", OdbcType.VarChar));
            _headerCols.Add("TAX_CHARGE");
            _headerParams.Add(new OdbcParameter("@TAX_CHARGE", OdbcType.Decimal));
            _headerCols.Add("SHIPPING_CHARGE");
            _headerParams.Add(new OdbcParameter("@SHIPPING_CHARGE", OdbcType.Decimal));
            _headerCols.Add("TOTAL");
            _headerParams.Add(new OdbcParameter("@TOTAL", OdbcType.Decimal));
            _headerCols.Add("BILL_CVS");
            _headerParams.Add(new OdbcParameter("@BILL_CVS", OdbcType.VarChar));
            _headerCols.Add("IP");
            _headerParams.Add(new OdbcParameter("@IP", OdbcType.VarChar));
            _headerCols.Add("GIFT_CARD");
            _headerParams.Add(new OdbcParameter("@GIFT_CARD", OdbcType.VarChar));
            _headerCols.Add("GIFT_AMT_USED");
            _headerParams.Add(new OdbcParameter("@GIFT_AMT_USED", OdbcType.Decimal));
            _headerCols.Add("AUTH");
            _headerParams.Add(new OdbcParameter("@AUTH", OdbcType.VarChar));
            _headerCols.Add("HOME_DELIVERY");
            _headerParams.Add(new OdbcParameter("@HOME_DELIVERY", OdbcType.VarChar));
			_headerCols.Add("CC_REFNO");
			_headerParams.Add(new OdbcParameter("@CC_REFNO", OdbcType.VarChar));

			_detailCols.Add("ID");
            _detailParams.Add(new OdbcParameter("@ID", OdbcType.VarChar));
            _detailCols.Add("ITEM_LINE");
            _detailParams.Add(new OdbcParameter("@ITEM_LINE", OdbcType.Int));
            _detailCols.Add("PKG_CNTR");
            _detailParams.Add(new OdbcParameter("@PKG_CNTR", OdbcType.VarChar));
            _detailCols.Add("ITEM_ID");
            _detailParams.Add(new OdbcParameter("@ITEM_ID", OdbcType.VarChar));
            _detailCols.Add("ITEM_CODE");
            _detailParams.Add(new OdbcParameter("@ITEM_CODE", OdbcType.VarChar));
            _detailCols.Add("ITEM_QUANTITY");
            _detailParams.Add(new OdbcParameter("@ITEM_QUANTITY", OdbcType.Int));
            _detailCols.Add("ITEM_UNIT_PRICE");
            _detailParams.Add(new OdbcParameter("@ITEM_UNIT_PRICE", OdbcType.Decimal));
            _detailCols.Add("ITEM_DESCRIPTION");
            _detailParams.Add(new OdbcParameter("@ITEM_DESCRIPTION", OdbcType.VarChar));
            _detailCols.Add("ITEM_URL");
            _detailParams.Add(new OdbcParameter("@ITEM_URL", OdbcType.VarChar));
            _detailCols.Add("PICKUP_BRANCH");
            _detailParams.Add(new OdbcParameter("@PICKUP_BRANCH", OdbcType.VarChar));
            _detailCols.Add("PICKUP_DROP");
            _detailParams.Add(new OdbcParameter("@PICKUP_DROP", OdbcType.VarChar));

            _shiptoCols.Add("ID");
            _shiptoParams.Add(new OdbcParameter("@ID", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_FULL");
            _shiptoParams.Add(new OdbcParameter("@SHIP_FULL", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_FIRST");
            _shiptoParams.Add(new OdbcParameter("@SHIP_FIRST", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_LAST");
            _shiptoParams.Add(new OdbcParameter("@SHIP_LAST", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_ADDRESS_1");
            _shiptoParams.Add(new OdbcParameter("@SHIP_ADDRESS_1", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_ADDRESS_2");
            _shiptoParams.Add(new OdbcParameter("@SHIP_ADDRESS_2", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_CITY");
            _shiptoParams.Add(new OdbcParameter("@SHIP_CITY", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_STATE");
            _shiptoParams.Add(new OdbcParameter("@SHIP_STATE", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_ZIP");
            _shiptoParams.Add(new OdbcParameter("@SHIP_ZIP", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_COUNTRY");
            _shiptoParams.Add(new OdbcParameter("@SHIP_COUNTRY", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_PHONE");
            _shiptoParams.Add(new OdbcParameter("@SHIP_PHONE", OdbcType.VarChar));
            _shiptoCols.Add("SHIP_EMAIL");
            _shiptoParams.Add(new OdbcParameter("@SHIP_EMAIL", OdbcType.VarChar));
            _shiptoCols.Add("SHIPPING");
            _shiptoParams.Add(new OdbcParameter("@SHIPPING", OdbcType.VarChar));
            _shiptoCols.Add("COMMENTS");
            _shiptoParams.Add(new OdbcParameter("@COMMENTS", OdbcType.VarChar));
        }

        /// <summary>
        /// Inserts an entire order from nopcommerce into isam
        /// </summary>
        /// <param name="order"></param>
        public void InsertOrder(Order order)
        {
            
            List<OrderItem> orderItemList = _orderService.GetOrderItems(order.Id).ToList();
            List<OrderItem> pickupOrderItems = new List<OrderItem>();
            List<OrderItem> shippingOrderItems = new List<OrderItem>();

            foreach (OrderItem item in orderItemList)
            {
                if (IsPickupItem(item))
                {
                    pickupOrderItems.Add(item);
                }
                else
                {
                    shippingOrderItems.Add(item);
                }
            }

            // store amount used before insert order
            // insert order changes the amount in the object memory to properly calculate gift card amounts
            // for multiple orders
            decimal giftCardAmtUsed = 0;
            var giftCardUsageHistory = _giftCardService.GetGiftCardUsageHistory(order);
            if (giftCardUsageHistory.Any())
            {
                giftCardAmtUsed = giftCardUsageHistory.OrderByDescending(gcu => gcu.CreatedOnUtc).FirstOrDefault().UsedValue;
            }

            InsertOrderOfType(order, shippingOrderItems, SHIP_FLAG);
            InsertOrderOfType(order, pickupOrderItems, PICKUP_IN_STORE_FLAG);

            // if there is a gift card, update gift card amt in isam
            if (giftCardUsageHistory.Any())
            {
                GiftCardUsageHistory orderGcUsage = giftCardUsageHistory.FirstOrDefault();
                GiftCard orderGiftCard = _giftCardService.GetGiftCardById(orderGcUsage.GiftCardId);

                var isamGiftCardInfo = _isamGiftCardService.GetGiftCardInfo(orderGiftCard.GiftCardCouponCode);
                GiftCard isamGiftCard = isamGiftCardInfo.GiftCard;
                decimal isamGiftCardAmtUsed = isamGiftCardInfo.AmountUsed;
                _isamGiftCardService.UpdateGiftCardAmt(isamGiftCard, isamGiftCardAmtUsed + giftCardAmtUsed);

                _giftCardUsageHistoryRepository.Delete(orderGcUsage);
                _giftCardService.UpdateGiftCard(orderGiftCard);
                
            }
        }

        /// <summary>CARRIER_
        /// Inserts a specific type of order into ISAM (home delivery/pickup/shipped)
        /// </summary>
        /// <param name="order"></param>
        /// <param name="orderItemList"></param>
        /// <param name="orderTypeFlag">which kind of order to place (home delivery/pickup/shipped)</param>
        private void InsertOrderOfType(Order order, List<OrderItem> orderItemList, char orderTypeFlag)
        {
            if (!orderItemList.Any()) return;

            GiftCardUsageHistory gcUsage = _giftCardService.GetGiftCardUsageHistory(order).FirstOrDefault();
            // setup for order
            List<string> values = GetYahooHeaderRowValues(order, orderItemList, orderTypeFlag, gcUsage);
            InsertUsingService(HEADER_TABLE_NAME, _headerCols, _headerParams, values);

            // setup for order item
            int itemLine = 1;
            foreach (OrderItem item in orderItemList)
            {
                var warranty = item.GetWarranty();
                if (warranty != null)
                {
                    // adjust price for item
                    item.UnitPriceExclTax -= warranty.PriceAdjustment;
                }

                values = GetYahooDetailRowValues(order, item, itemLine, orderTypeFlag);
                InsertUsingService(DETAIL_TABLE_NAME, _detailCols, _detailParams, values);

                if (warranty != null)
                {
                    ++itemLine;
                    var warrantyValues = GetYahooDetailRowValuesForWarranty(
                        order,
                        item,
                        itemLine,
                        orderTypeFlag,
                        warranty
                    );
                    InsertUsingService(
                        DETAIL_TABLE_NAME,
                        _detailCols,
                        _detailParams,
                        warrantyValues
                    );
                }

                ++itemLine;
            }

            // setup for shipto
            values = GetYahooShiptoRowValues(order, orderTypeFlag);
            InsertUsingService(SHIPTO_TABLE_NAME, _shiptoCols, _shiptoParams, values);

            // execute all inserts in a batch
            _baseIsamService.ExecuteBatch();
        }

        private void InsertUsingService(string tableName, List<string> cols, List<OdbcParameter> colParams, List<string> values)
        {
            if (colParams.Count != values.Count)
            {
                throw new Exception("coder you messed up, colparam size must equal value size, c: " + colParams.Count + " v: " + values.Count);
            }

            List<string> insertCols = new List<string>();
            List<OdbcParameter> insertParams = new List<OdbcParameter>();

            for (int i = 0; i < values.Count; ++i)
            {
                if (!String.IsNullOrEmpty(values[i]))
                {
                    colParams[i].Value = values[i];
                    insertParams.Add(colParams[i]);
                    insertCols.Add(cols[i]);
                }
                else if (colParams[i].OdbcType == OdbcType.VarChar && !_canBeNullSet.Contains(cols[i]))
                {
                    colParams[i].Value = " ";
                    insertParams.Add(colParams[i]);
                    insertCols.Add(cols[i]);
                }
            }

            _baseIsamService.Insert(_settings.TablePrefix + tableName, insertCols, insertParams, true);
        }

        #region Nop Order -> Yahoo table

        private List<string> GetYahooHeaderRowValues(Order order, List<OrderItem> orderItems, char orderTypeFlag, GiftCardUsageHistory gcUsage)
        {
            // prepare all values

            // -------------credit card values--------------
            string encryptionKey = _settingService.GetSettingByKey<string>("securitysettings.encryptionkey");

            string cardNumber = " ";
            string cardExpiry = " ";
            string cardCvv2 = " ";
            string cardType = " ";

            if (!string.IsNullOrEmpty(order.CardNumber))
            {
                cardNumber = _encryptionService.DecryptText(order.CardNumber, encryptionKey);
            }

            if (!string.IsNullOrEmpty(order.CardExpirationMonth) && !string.IsNullOrEmpty(order.CardExpirationYear))
            {
                string cardMonth = _encryptionService.DecryptText(order.CardExpirationMonth, encryptionKey);
                string cardYear = _encryptionService.DecryptText(order.CardExpirationYear, encryptionKey);
                cardExpiry = cardYear.Equals("") ? "" : (cardMonth + "/" + cardYear);
            }

            if (!string.IsNullOrEmpty(order.CardCvv2))
            {
                cardCvv2 = _encryptionService.DecryptText(order.CardCvv2, encryptionKey);
            }

            if (!string.IsNullOrEmpty(order.CardName))
            {
                cardType = _encryptionService.DecryptText(order.CardName, encryptionKey);
            }

            string[] creditCardInfo =
            {
                cardType, cardNumber, cardExpiry
            };

            // --------shipping-------------

            decimal homeDeliveryCost = 0;
            decimal shippingCost = 0;

            // if order is shipping, add shipping costs
            if (orderTypeFlag == SHIP_FLAG)
            {
                // split up shipping cost between home delivery & shipping item
                decimal homeDeliveryCostPerItem = 14.75M;

                homeDeliveryCost = 0;
                foreach (OrderItem item in orderItems)
                {
                    if (IsHomeDeliveryItem(item))
                    {
                        homeDeliveryCost += homeDeliveryCostPerItem * item.Quantity;
                    }
                }

                shippingCost = order.OrderShippingExclTax - homeDeliveryCost;
            }
            shippingCost = _priceCalculationService.RoundPrice(shippingCost);
            homeDeliveryCost = _priceCalculationService.RoundPrice(homeDeliveryCost);

            // ---------pricing-----------------
            decimal backendOrderTax = 0;
            decimal backendOrderTotal = 0;
            foreach(OrderItem item in orderItems)
            {
                backendOrderTax += item.PriceInclTax - item.PriceExclTax;
                backendOrderTotal += item.PriceInclTax;
            }

            if (orderTypeFlag == SHIP_FLAG)
            {
                decimal shippingTax = order.OrderShippingInclTax - order.OrderShippingExclTax;
                backendOrderTax += shippingTax;
                backendOrderTotal += order.OrderShippingInclTax;
            }
            backendOrderTax = _priceCalculationService.RoundPrice(backendOrderTax);
            backendOrderTotal = _priceCalculationService.RoundPrice(backendOrderTotal);

            // ---------gift card-------------

            string giftCardCode = "";
            decimal giftCardUsed = 0;
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

            List<string> values = new List<string>();
            values.Add(GenerateAbcOrderId(order.Id, orderTypeFlag));

            values.Add(order.CreatedOnUtc.ToString());
            values.AddRange(GetAddressValues(_addressService.GetAddressById(order.BillingAddressId)));

            values.AddRange(creditCardInfo);

            // tax
            values.Add(backendOrderTax.ToString());

            // shipping (fedex)
            values.Add(shippingCost.ToString());

            // order total
            values.Add(backendOrderTotal.ToString());

            // credit card csv/cvv2
            values.Add(cardCvv2);

            // ip address - strip everything after ','
            string customerIp = order.CustomerIp;
            int index = customerIp.IndexOf(",");
            if (index > 0)
                customerIp = customerIp.Substring(0, index-1);
            values.Add(customerIp);

            // GIFT_CARD, GIFT_AMT_USED, 
            values.Add(giftCardCode);
            values.Add(giftCardUsed.ToString());

            // AUTH
            values.Add(order.AuthorizationTransactionCode);

            // HOME_DELIVERY (home delivery shipping cost)
            values.Add(homeDeliveryCost.ToString());

			// CC_REFNO
		    values.Add(order.GetCardRefNo());

            return values;
        }

        private List<string> GetYahooDetailRowValuesForWarranty(
            Order order,
            OrderItem orderItem,
            int orderItemLine,
            char orderTypeFlag,
            ProductAttributeValue warranty
        )
        {
            var values = GetYahooDetailRowValues(
                order,
                orderItem,
                orderItemLine,
                orderTypeFlag
            );

            // now edit those values based on what the warranty format should be
            // based on the index
            // consider putting this whole thing into an object
            var product = _productService.GetProductById(orderItem.ProductId);
            string abcItemNumber = _productAbcDescriptionRepository.Table
                    .Where(pad => pad.Product_Id == orderItem.ProductId)
                    .Select(pad => pad.AbcItemNumber).FirstOrDefault();


            const int itemIdIndex = 3;
            const int itemCodeIndex = 4;
            const int itemUnitPriceIndex = 6;
            const int itemDescriptionIndex = 7;
            const int itemUrlIndex = 8;
            const int pickupBranchIndex = 9;

            values[itemIdIndex] = abcItemNumber;
            values[itemCodeIndex] = GetWarrantySku(warranty);
            values[itemUnitPriceIndex] = 
                decimal.Round(
                    warranty.PriceAdjustment,
                    2,
                    MidpointRounding.AwayFromZero
                ).ToString("N");
            values[itemDescriptionIndex] = warranty.Name;
            values[itemUrlIndex] = "";
            values[pickupBranchIndex] = GetPickupStore(orderItem);

            return values;
        }

        private List<string> GetYahooDetailRowValues(
            Order order,
            OrderItem orderItem,
            int orderItemLine,
            char orderTypeFlag
        )
        {
            List<string> values = new List<string>();
            values.Add(GenerateAbcOrderId(order.Id, orderTypeFlag));

            // PKG_CNTR = 2 spaces (unused; it's for USA Appliances)
            string itemUrl = String.Empty;

            string itemId = String.Empty;
            string itemCode = String.Empty;

            var product = _productService.GetProductById(orderItem.ProductId);
            itemUrl = _storeContext.CurrentStore.Url + _urlRecordService.GetSeName(product);

            // item id = model number
            itemId = product.Sku;

            // item code = isam id
            itemCode = _productAbcDescriptionRepository.Table
                .Where(pad => pad.Product_Id == orderItem.ProductId)
                .Select(pad => pad.AbcItemNumber).FirstOrDefault();
            if (String.IsNullOrEmpty(itemCode))
            {
                // if no isam id then fill with model number
                itemCode = product.Sku;
            }

            string abcShop = GetPickupStore(orderItem);

            string[] additionalValues =
            {
                // ID                ITEM_LINE, ITEM_ID,    ITEM_CODE,             ITEM_QUANTITY
                orderItemLine.ToString(), "  ", itemId, itemCode, orderItem.Quantity.ToString(),
                orderItem.UnitPriceExclTax.ToString(), product.Name,
                // ITEM_URL, PICKUP_BRANCH, PICKUP_DROP
                itemUrl, abcShop, ""
            };
            values.AddRange(additionalValues);

            return values;
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

        private List<string> GetYahooShiptoRowValues(Order order, char orderTypeFlag)
        {
            List<string> values = new List<string>();
            values.Add(GenerateAbcOrderId(order.Id, orderTypeFlag));
            var shippingAddress = _addressService.GetAddressById(order.ShippingAddressId.Value);
            if (orderTypeFlag == PICKUP_IN_STORE_FLAG || shippingAddress == null)
            {
                // 3 missing values => id, shipping, comments
                for (int i = 0; i < _shiptoParams.Count - 3; ++i)
                {
                    values.Add("");
                }
            }
            else
            {
                values.AddRange(GetAddressValues(shippingAddress));
            }
            // SHIPPING
            values.Add("");
            // COMMENTS
            values.Add("");

            return values;
        }

        // Takes an address, returns the string values (as required by ODBC)
        private string[] GetAddressValues(Address address)
        {
            string[] values =
            {
                address.FirstName + " " + address.LastName, address.FirstName, address.LastName,
                address.Address1, address.Address2, address.City, _stateProvinceService.GetStateProvinceById(address.StateProvinceId.Value).Abbreviation,
                address.ZipPostalCode, _countryService.GetCountryById(address.CountryId.Value).ThreeLetterIsoCode, address.PhoneNumber, address.Email,
            };
            return values;
        }
        
        private string GenerateAbcOrderId(int orderId, char orderTypeFlag)
        {
            return _settings.OrderIdPrefix + orderId + "+" + orderTypeFlag;
        }

        #endregion

        #region Check Item Type Utilities

        private bool IsPickupAttribute(ProductAttribute productAttribute)
        {
            return productAttribute.Name == "Pickup";
        }

        private bool IsHomeDeliveryAttribute(ProductAttribute productAttribute)
        {
            return productAttribute.Name == "Home Delivery";
        }

        private bool IsPickupItem(OrderItem orderItem)
        {
            return _attributeUtilities.GetPickupAttributeMapping(orderItem.AttributesXml) != null;
        }

        private bool IsHomeDeliveryItem(OrderItem orderItem)
        {
            return _attributeUtilities.GetHomeDeliveryAttributeMapping(orderItem.AttributesXml) != null;
        }

        #endregion

        private string GetWarrantySku(ProductAttributeValue warranty)
        {
            return _warrantySkuRepository.Table
                .Where(ws => ws.Name == warranty.Name)
                .Select(ws => ws.Sku).FirstOrDefault();
        }
    }
}
