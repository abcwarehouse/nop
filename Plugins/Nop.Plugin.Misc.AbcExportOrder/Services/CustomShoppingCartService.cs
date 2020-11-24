using Nop.Services.Orders;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;
using Nop.Services.Shipping.Date;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Data;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Nop.Services.Caching;
using SevenSpikes.Nop.Plugins.StoreLocator.Services;
using Nop.Services.Shipping;
using Nop.Core.Caching;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Services.Seo;
using SevenSpikes.Nop.Plugins.StoreLocator.Domain.Shops;
using Nop.Plugin.Misc.AbcCore.Models;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public class CustomShoppingCartService : ShoppingCartService
    {
        private readonly ICustomerShopService _customerShopService;
        private readonly IAttributeUtilities _attributeUtilities;
        private readonly IBackendStockService _backendStockService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IShopService _shopService;
        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;

        public CustomShoppingCartService(
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
            IShoppingCartService shoppingCartService
        ) : base(catalogSettings, aclService, actionContextAccessor, cacheKeyService, checkoutAttributeParser,
                 checkoutAttributeService, currencyService, customerService, dateRangeService, dateTimeHelper,
                 eventPublisher, genericAttributeService, localizationService, permissionService, priceCalculationService,
                 priceFormatter, productAttributeParser, productAttributeService, productService, sciRepository,
                 shippingService, staticCacheManager, storeContext, storeMappingService, urlHelperFactory,
                 urlRecordService, workContext, orderSettings, shoppingCartSettings)
        {
            _customerShopService = EngineContext.Current.Resolve<ICustomerShopService>();
            _attributeUtilities = EngineContext.Current.Resolve<IAttributeUtilities>();
            _backendStockService = EngineContext.Current.Resolve<IBackendStockService>();
            _shopService = EngineContext.Current.Resolve<IShopService>();
            _productAttributeParser = productAttributeParser;
            _productService = productService;
            _shoppingCartService = shoppingCartService;
        }

        public override void MigrateShoppingCart(Customer fromCustomer, Customer toCustomer, bool includeCouponCodes)
        {
            base.MigrateShoppingCart(fromCustomer, toCustomer, includeCouponCodes);

            if (fromCustomer.Id == toCustomer.Id)
                return;

            var fromCsm = _customerShopService.GetCurrentCustomerShopMapping(fromCustomer.Id);

            if (fromCsm == null)
            {
                //old customer has no pickup items to update, do nothing
                return;
            }

            //update tocustomer's shop mapping
            _customerShopService.InsertOrUpdateCustomerShop(toCustomer.Id, fromCsm.ShopId);
            var csm = _customerShopService.GetCurrentCustomerShopMapping(toCustomer.Id);
            Shop shop = _shopService.GetShopById(csm.ShopId);

            //used to merge together products that will now have the same attributes
            Dictionary<int, ShoppingCartItem> productIdToPickupSci = new Dictionary<int, ShoppingCartItem>();
            Dictionary<int, ShoppingCartItem> productIdToDeliverySci = new Dictionary<int, ShoppingCartItem>();

            List<ShoppingCartItem> toDelete = new List<ShoppingCartItem>();

            //update all pickup items in the cart to current availability status
            foreach (ShoppingCartItem sci in _shoppingCartService.GetShoppingCart(toCustomer).Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart))
            {

                ProductAttributeMapping pickupAttribute = _attributeUtilities.GetPickupAttributeMapping(sci.AttributesXml);

                if (pickupAttribute != null)
                {
                    //if we already have an existing shoppingcart item for this product update its quantity
                    if (productIdToPickupSci.ContainsKey(sci.ProductId))
                    {
                        var existingSci = productIdToPickupSci[sci.ProductId];
                        base.UpdateShoppingCartItem(toCustomer, existingSci.Id,
                            existingSci.AttributesXml, existingSci.CustomerEnteredPrice, null, null, existingSci.Quantity + sci.Quantity, false);
                        toDelete.Add(sci);
                    }
                    else
                    {
                        // check if product is available at the selected store
                        StockResponse stockResponse = _backendStockService.GetApiStock(sci.ProductId);
                        bool available = false;
                        if (stockResponse != null)
                        {
                            available = stockResponse.ProductStocks.Where(ps => ps.Available && ps.Shop.Id == csm.ShopId).Any();
                        }

                        //if available clean and re add the pickup attribute
                        if (available)
                        {
                            string removedAttr = _productAttributeParser.RemoveProductAttribute(sci.AttributesXml, pickupAttribute);

                            sci.AttributesXml = _attributeUtilities.InsertPickupAttribute(_productService.GetProductById(sci.ProductId), stockResponse, removedAttr, shop);
                            productIdToPickupSci[sci.ProductId] = sci;
                        }
                        else
                        {
                            //else we switch it to home delivery
                            //merge home delivery if it exists
                            if (productIdToDeliverySci.ContainsKey(sci.ProductId))
                            {
                                var existingSci = productIdToDeliverySci[sci.ProductId];
                                base.UpdateShoppingCartItem(toCustomer, existingSci.Id,
                                    existingSci.AttributesXml, existingSci.CustomerEnteredPrice, null, null, existingSci.Quantity + sci.Quantity, false);
                                toDelete.Add(sci);
                                continue;
                            }
                            else
                            {
                                //else replace the pickup attribute with home delivery
                                sci.AttributesXml = _attributeUtilities.InsertHomeDeliveryAttribute(_productService.GetProductById(sci.ProductId), sci.AttributesXml);
                                productIdToDeliverySci[sci.ProductId] = sci;
                            }

                            
                        }
                        //update the sci with new attributes
                        base.UpdateShoppingCartItem(toCustomer, sci.Id,
                                sci.AttributesXml, sci.CustomerEnteredPrice, null, null, sci.Quantity, false);
                    }

                }
                else
                {
                    //if not a pickup item, keep track for later merging
                    productIdToDeliverySci[sci.ProductId] = sci;
                }
            }

            for(int i = 0; i < toDelete.Count; ++i)
            {
                base.DeleteShoppingCartItem(toDelete[i]);
            }


        }
    }
}
