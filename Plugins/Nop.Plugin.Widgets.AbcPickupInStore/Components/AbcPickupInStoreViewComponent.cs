using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Widgets.AbcPickupInStore;
using Nop.Plugin.Widgets.AbcPickupInStore.Models;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;
using SevenSpikes.Nop.Plugins.StoreLocator.Domain;
using SevenSpikes.Nop.Plugins.StoreLocator.Services;

namespace Nop.Plugin.Widgets.AbcPickupInStore.Components
{
    [ViewComponent(Name = "AbcPickupInStore")]
    public class AbcPickupInStoreViewComponent : NopViewComponent
    {
        private readonly string DELIVERY_SELECTION_WIDGET_ZONE =
            "productdetails_overview_bottom";
        private readonly string PICKUP_INFO_WIDGET_ZONE =
            "productdetails_before_tabs";

        private readonly ILogger _logger;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly ICustomerShopService _customerShopService;
        private readonly IShopService _shopService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly PickupInStoreSettings _pickupInStoreSettings;
        private readonly StoreLocatorSettings _storeLocatorSettings;


        public AbcPickupInStoreViewComponent(
            ILogger logger,
            IStoreContext storeContext,
            IWorkContext workContext,
            IProductService productService,
            IProductAttributeService productAttributeService,
            ICustomerShopService customerShopService,
            IShopService shopService,
            IShoppingCartService shoppingCartService,
            PickupInStoreSettings pickupInStoreSettings,
            StoreLocatorSettings storeLocatorSettings
        )
        {
            _logger = logger;
            _storeContext = storeContext;
            _workContext = workContext;
            _productService = productService;
            _productAttributeService = productAttributeService;
            _customerShopService = customerShopService;
            _shopService = shopService;
            _shoppingCartService = shoppingCartService;
            _pickupInStoreSettings = pickupInStoreSettings;
            _storeLocatorSettings = storeLocatorSettings;
        }
        public IViewComponentResult Invoke(
            string widgetZone,
            ProductDetailsModel additionalData = null
        )
        {
            int productId = -1;
            if (additionalData != null)
            {
                productId = additionalData.Id;
            }

            // clearance store specific
            if (_storeContext.CurrentStore.Url.Contains("clearance"))
            {
                if (widgetZone == PICKUP_INFO_WIDGET_ZONE)
                {
                    return View(
                        "~/Plugins/Widgets.AbcPickupInStore/Views/ClearanceStoreStockContainer.cshtml",
                        productId
                    );
                }
                else
                {
                    return Content("");
                }
            }
            // normal abc store
            else
            {
                Product product = _productService.GetProductById(productId);
                if (productId > 0)
                {
                    // if the buy button is disabled, do not show any UI about
                    // products
                    if (product.DisableBuyButton || !product.Published)
                    {
                        return Content("");
                    }
                }

                PickStoreModel model = InitializePickStoreModel();
                model.ProductId = productId;

                if (string.IsNullOrWhiteSpace(model.GoogleMapsAPIKey))
                {
                    _logger.Warning(
                        "Google Maps API Key not included in Store Locator " +
                        "settings, will have issues with ABC Pickup in Store");
                }

                // if item is pickup in store, then display the UI for pickup
                // in store
                if (_productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                        .Where(pam => _productAttributeService.GetProductAttributeById(pam.ProductAttributeId).Name == "Pickup").Any())
                {
                    string url = "";
                    if (widgetZone == PICKUP_INFO_WIDGET_ZONE)
                    {
                        url = "~/Plugins/Widgets.AbcPickupInStore/Views/PickupInStoreContainer.cshtml";
                    }
                    else if (widgetZone == DELIVERY_SELECTION_WIDGET_ZONE)
                    {
                        url = "~/Plugins/Widgets.AbcPickupInStore/Views/SelectDeliveryMethod.cshtml";
                    }
                    return View(url, model);
                }
                else
                {
                    return Content("");
                }
            }
        }

        private PickStoreModel InitializePickStoreModel()
        {
            // initialize model for the view
            PickStoreModel model = new PickStoreModel();

            // get the store that the customer selected previously if selected
            // at all
            CustomerShopMapping currentCustomerShopMapping
                = _customerShopService.GetCurrentCustomerShopMapping(
                    _workContext.CurrentCustomer.Id
                );

            var shoppingCartItems = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer);

            // if selected before, add it to the model, else -1
            if (currentCustomerShopMapping != null)
            {
                model.SelectedShop = _shopService.GetShopById(currentCustomerShopMapping.ShopId);
            }

            model.PickupInStoreText = _pickupInStoreSettings.PickupInStoreText;
            model.GoogleMapsAPIKey = _storeLocatorSettings.GoogleApiKey;

            return model;
        }
    }
}
