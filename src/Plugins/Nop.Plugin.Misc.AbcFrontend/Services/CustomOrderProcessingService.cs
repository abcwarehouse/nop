using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Core.Domain.Discounts;
using Nop.Core.Events;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public class CustomOrderProcessingService : OrderProcessingService
    {
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IProductService _productService;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ITaxService _taxService;
        private readonly PaymentSettings _paymentSettings;

        // custom
        private readonly IWarrantyService _warrantyService;

        public CustomOrderProcessingService(
            CurrencySettings currencySettings,
            IAddressService addressService,
            IAffiliateService affiliateService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            ICountryService countryService,
            ICurrencyService currencyService,
            ICustomerActivityService customerActivityService,
            ICustomerService customerService,
            ICustomNumberFormatter customNumberFormatter,
            IDiscountService discountService,
            IEncryptionService encryptionService,
            IEventPublisher eventPublisher,
            IGenericAttributeService genericAttributeService,
            IGiftCardService giftCardService,
            ILanguageService languageService,
            ILocalizationService localizationService,
            ILogger logger,
            IOrderService orderService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPaymentPluginManager paymentPluginManager,
            IPaymentService paymentService,
            IPdfService pdfService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeFormatter productAttributeFormatter,
            IProductAttributeParser productAttributeParser,
            IProductService productService,
            IRewardPointService rewardPointService,
            IShipmentService shipmentService,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            IStateProvinceService stateProvinceService,
            ITaxService taxService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings localizationSettings,
            OrderSettings orderSettings,
            PaymentSettings paymentSettings,
            RewardPointsSettings rewardPointsSettings,
            ShippingSettings shippingSettings,
            TaxSettings taxSettings,
            // custom
            IWarrantyService warrantyService
        ) : base(currencySettings, addressService, affiliateService, checkoutAttributeFormatter,
                   countryService, currencyService, customerActivityService, customerService,
                   customNumberFormatter, discountService, encryptionService,
                   eventPublisher, genericAttributeService, giftCardService,
                   languageService, localizationService, logger, orderService,
                   orderTotalCalculationService, paymentPluginManager, paymentService,
                   pdfService, priceCalculationService, priceFormatter,
                   productAttributeFormatter, productAttributeParser, productService,
                   rewardPointService, shipmentService, shippingService, shoppingCartService,
                   stateProvinceService, taxService, vendorService, webHelper, workContext,
                   workflowMessageService, localizationSettings, orderSettings,
                   paymentSettings, rewardPointsSettings, shippingSettings, taxSettings)
        {
            _customerActivityService = customerActivityService;
            _customerService = customerService;
            _discountService = discountService;
            _eventPublisher = eventPublisher;
            _localizationService = localizationService;
            _logger = logger;
            _orderService = orderService;
            _paymentService = paymentService;
            _priceCalculationService = priceCalculationService;
            _productAttributeFormatter = productAttributeFormatter;
            _productService = productService;
            _shippingService = shippingService;
            _shoppingCartService = shoppingCartService;
            _taxService = taxService;
            _paymentSettings = paymentSettings;

            // custom
            _warrantyService = warrantyService;
        }

        protected async override Task MoveShoppingCartItemsToOrderItemsAsync(
            PlaceOrderContainer details,
            Order order
        )
        {
            //move shopping cart items to order items
            foreach (var sc in details.Cart)
            {
                var product = await _productService.GetProductByIdAsync(sc.ProductId);

                //prices
                var scUnitPrice = await _shoppingCartService.GetUnitPriceAsync(sc);
                var scSubTotal = _shoppingCartService.GetSubTotal(sc, true, out var discountAmount,
                    out var scDiscounts, out _);
                var scUnitPriceInclTax =
                    _taxService.GetProductPrice(product, scUnitPrice, true, details.Customer, out var _);
                var scUnitPriceExclTax =
                    _taxService.GetProductPrice(product, scUnitPrice, false, details.Customer, out _);
                var scSubTotalInclTax =
                    _taxService.GetProductPrice(product, scSubTotal, true, details.Customer, out _);
                var scSubTotalExclTax =
                    _taxService.GetProductPrice(product, scSubTotal, false, details.Customer, out _);


                decimal warrantyUnitPriceExclTax;
                decimal warrantyUnitPriceInclTax;

                // custom - getting warranty tax
                decimal taxRate;
                _warrantyService.CalculateWarrantyTax(sc, details.Customer, scSubTotalExclTax, scUnitPriceExclTax,
                    out taxRate,
                    out scSubTotalInclTax, out scUnitPriceInclTax,
                    out warrantyUnitPriceExclTax, out warrantyUnitPriceInclTax);

                var discountAmountInclTax =
                    _taxService.GetProductPrice(product, discountAmount, true, details.Customer, out _);
                var discountAmountExclTax =
                    _taxService.GetProductPrice(product, discountAmount, false, details.Customer, out _);
                foreach (var disc in scDiscounts)
                    if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
                        details.AppliedDiscounts.Add(disc);

                //attributes
                var attributeDescription =
                    _productAttributeFormatter.FormatAttributes(product, sc.AttributesXml, details.Customer);

                var itemWeight = _shippingService.GetShoppingCartItemWeight(sc);

                //save order item
                var orderItem = new OrderItem
                {
                    OrderItemGuid = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    UnitPriceInclTax = scUnitPriceInclTax,
                    UnitPriceExclTax = scUnitPriceExclTax,
                    PriceInclTax = scSubTotalInclTax,
                    PriceExclTax = scSubTotalExclTax,
                    OriginalProductCost = _priceCalculationService.GetProductCost(product, sc.AttributesXml),
                    AttributeDescription = attributeDescription,
                    AttributesXml = sc.AttributesXml,
                    Quantity = sc.Quantity,
                    DiscountAmountInclTax = discountAmountInclTax,
                    DiscountAmountExclTax = discountAmountExclTax,
                    DownloadCount = 0,
                    IsDownloadActivated = false,
                    LicenseDownloadId = 0,
                    ItemWeight = itemWeight,
                    RentalStartDateUtc = sc.RentalStartDateUtc,
                    RentalEndDateUtc = sc.RentalEndDateUtc
                };

                _orderService.InsertOrderItem(orderItem);

                //gift cards
                AddGiftCards(product, sc.AttributesXml, sc.Quantity, orderItem, scUnitPriceExclTax);

                //inventory
                _productService.AdjustInventory(product, -sc.Quantity, sc.AttributesXml,
                    string.Format(await _localizationService.GetResourceAsync( "Admin.StockQuantityHistory.Messages.PlaceOrder"), order.Id));
            }

            //clear shopping cart
            details.Cart.ToList().ForEach(sci => await _shoppingCartService.DeleteShoppingCartItemAsync(sci, false));
        }
    }
}
