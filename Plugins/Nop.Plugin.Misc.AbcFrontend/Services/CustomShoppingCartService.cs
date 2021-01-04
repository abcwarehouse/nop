using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Discounts;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Services.Directory;
using Nop.Services.Customers;
using Nop.Services.Caching;
using Nop.Services.Orders;
using Nop.Core.Domain.Discounts;
using Nop.Services.Security;
using Nop.Services.Shipping.Date;
using Nop.Services.Common;
using Nop.Services.Stores;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Nop.Services.Helpers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Seo;
using Nop.Services.Shipping;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    //a custom price calculation service to modify cart price as needed
    class CustomShoppingCartService : ShoppingCartService
    {
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IAttributeUtilities _attributeUtilities;
        private readonly IRepository<HiddenAttributeValue> _hiddenAttributeValueRepository;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IPriceCalculationService _priceCalculationService;

        public CustomShoppingCartService(
            // base
            CatalogSettings catalogSettings,
            IAclService aclService,
            IActionContextAccessor actionContextAccessor,
            ICacheKeyService cacheKeyService,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICheckoutAttributeService checkoutAttributeService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IDateRangeService dateRangeService,
            IDateTimeHelper dateTimeHelper,
            IEventPublisher eventPublisher,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IRepository<ShoppingCartItem> sciRepository,
            IShippingService shippingService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IUrlHelperFactory urlHelperFactory,
            IUrlRecordService urlRecordService,
            IWorkContext workContext,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings,
            // custom
            IAttributeUtilities attributeUtilities,
            IRepository<HiddenAttributeValue> hiddenAttributeValueRepository
        )
            : base(catalogSettings, aclService, actionContextAccessor, cacheKeyService,
                checkoutAttributeParser, checkoutAttributeService, currencyService,
                customerService, dateRangeService, dateTimeHelper, eventPublisher,
                genericAttributeService, localizationService, permissionService,
                priceCalculationService, priceFormatter, productAttributeParser,
                productAttributeService, productService, sciRepository,
                shippingService, staticCacheManager, storeContext, storeMappingService,
                urlHelperFactory, urlRecordService, workContext, orderSettings,
                shoppingCartSettings)
        {
            try
            {
                _hiddenAttributeValueRepository =
                    EngineContext.Current.Resolve<IRepository<HiddenAttributeValue>>();
            }
            catch (Exception ex)
            {
                EngineContext.Current.Resolve<ILogger>().Error(
                    "Could not resolve the HiddenAttributeValue table " +
                    " (is Misc.AbcSync Installed?)", ex);
            }

            _productAttributeParser = productAttributeParser;
            _attributeUtilities = attributeUtilities;
            _shoppingCartSettings = shoppingCartSettings;
            _priceCalculationService = priceCalculationService;
        }

        /// <summary>
        /// Gets the shopping cart unit price (one item)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="attributesXml">Product atrributes (XML format)</param>
        /// <param name="customerEnteredPrice">Customer entered price (if specified)</param>
        /// <param name="rentalStartDate">Rental start date (null for not rental products)</param>
        /// <param name="rentalEndDate">Rental end date (null for not rental products)</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <param name="discountAmount">Applied discount amount</param>
        /// <param name="appliedDiscounts">Applied discounts</param>
        /// <returns>Shopping cart unit price (one item)</returns>
        public override decimal GetUnitPrice(Product product,
            Customer customer,
            ShoppingCartType shoppingCartType,
            int quantity,
            string attributesXml,
            decimal customerEnteredPrice,
            DateTime? rentalStartDate, DateTime? rentalEndDate,
            bool includeDiscounts,
            out decimal discountAmount,
            out List<Discount> appliedDiscounts)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            discountAmount = decimal.Zero;
            appliedDiscounts = new List<Discount>();

            decimal finalPrice;

            var combination = _productAttributeParser.FindProductAttributeCombination(product, attributesXml);
            if (combination?.OverriddenPrice.HasValue ?? false)
            {
                finalPrice = _priceCalculationService.GetFinalPrice(product,
                        customer,
                        combination.OverriddenPrice.Value,
                        decimal.Zero,
                        includeDiscounts,
                        quantity,
                        product.IsRental ? rentalStartDate : null,
                        product.IsRental ? rentalEndDate : null,
                        out discountAmount, out appliedDiscounts);
            }
            else
            {
                //summarize price of all attributes except warranties (warranties processed separately)
                decimal warrantyPrice = decimal.Zero;
                ProductAttributeMapping warrantyPam = _attributeUtilities.GetWarrantyAttributeMapping(attributesXml);

                decimal attributesTotalPrice = decimal.Zero;
                var attributeValues = _productAttributeParser.ParseProductAttributeValues(attributesXml);
                if (attributeValues != null)
                {
                    foreach (var attributeValue in attributeValues)
                    {
                        if (warrantyPam != null && attributeValue.ProductAttributeMappingId == warrantyPam.Id)
                        {
                            warrantyPrice =
                                _priceCalculationService.GetProductAttributeValuePriceAdjustment(
                                    product, attributeValue, customer, product.CustomerEntersPrice ? (decimal?)customerEnteredPrice : null
                                );
                        }
                        else
                        {
                            attributesTotalPrice +=
                                _priceCalculationService.GetProductAttributeValuePriceAdjustment(
                                    product, attributeValue, customer, product.CustomerEntersPrice ? (decimal?)customerEnteredPrice : null
                                );
                        }
                    }
                }

                //get price of a product (with previously calculated price of all attributes)
                if (product.CustomerEntersPrice)
                {
                    finalPrice = customerEnteredPrice;
                }
                else
                {
                    int qty;
                    if (_shoppingCartSettings.GroupTierPricesForDistinctShoppingCartItems)
                    {
                        //the same products with distinct product attributes could be stored as distinct "ShoppingCartItem" records
                        //so let's find how many of the current products are in the cart
                        qty = GetShoppingCart(
                            customer,
                            shoppingCartType: shoppingCartType,
                            productId: product.Id)
                            .Sum(x => x.Quantity);
                        if (qty == 0)
                        {
                            qty = quantity;
                        }
                    }
                    else
                    {
                        qty = quantity;
                    }
                    finalPrice = _priceCalculationService.GetFinalPrice(
                        product,
                        customer,
                        attributesTotalPrice,
                        includeDiscounts,
                        qty,
                        product.IsRental ? rentalStartDate : null,
                        product.IsRental ? rentalEndDate : null,
                        out discountAmount, out appliedDiscounts);
                    finalPrice += warrantyPrice;
                }
            }

            //rounding
            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                finalPrice = _priceCalculationService.RoundPrice(finalPrice);

            return finalPrice;
        }

        public override decimal GetUnitPrice(
            ShoppingCartItem shoppingCartItem,
            bool includeDiscounts,
            out decimal discountAmount,
            out List<Discount> appliedDiscounts)
        {
            var unitPrice = base.GetUnitPrice(
                shoppingCartItem,
                includeDiscounts,
                out discountAmount,
                out appliedDiscounts);

            if (_hiddenAttributeValueRepository == null)
            {
                return unitPrice;
            }

            var hiddenAttrVals = _hiddenAttributeValueRepository.Table.Where(hav => hav.ShoppingCartItem_Id == shoppingCartItem.Id);

            foreach (var hav in hiddenAttrVals)
            {
                unitPrice += hav.PriceAdjustment;
            }

            return unitPrice;
        }
    }
}
