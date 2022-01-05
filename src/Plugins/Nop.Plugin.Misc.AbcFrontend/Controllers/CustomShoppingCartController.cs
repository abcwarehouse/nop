﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Media;
using Nop.Web.Models.ShoppingCart;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Data;
using Nop.Core.Domain.Security;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Caching;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.AbcCore.Domain;
using Microsoft.AspNetCore.Http;
using Nop.Core.Html;
using Microsoft.Extensions.Primitives;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using System.Threading.Tasks;
using Nop.Plugin.Misc.AbcFrontend.Services;

namespace Nop.Plugin.Misc.AbcFrontend.Controllers
{
    public partial class CustomShoppingCartController : ShoppingCartController
    {
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ILocalizationService _localizationService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWorkContext _workContext;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        // custom
        private readonly IAddToCartSlideoutService _addToCartSlideoutService;
        private readonly IAttributeUtilities _attributeUtilities;
        private readonly IRepository<CustomerShopMapping> _customerShopMappingRepository;
        private readonly IBackendStockService _backendStockService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;

        public CustomShoppingCartController(
            CaptchaSettings captchaSettings,
            CustomerSettings customerSettings,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICheckoutAttributeService checkoutAttributeService,
            ICurrencyService currencyService,
            ICustomerActivityService customerActivityService,
            ICustomerService customerService,
            IDiscountService discountService,
            IDownloadService downloadService,
            IGenericAttributeService genericAttributeService,
            IGiftCardService giftCardService,
            ILocalizationService localizationService,
            INopFileProvider fileProvider,
            INotificationService notificationService,
            IPermissionService permissionService,
            IPictureService pictureService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IShippingService shippingService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            IShoppingCartService shoppingCartService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            ITaxService taxService,
            IUrlRecordService urlRecordService,
            IWebHelper webHelper,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            MediaSettings mediaSettings,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings,
            ShippingSettings shippingSettings,
            // custom
            IAddToCartSlideoutService addToCartSlideoutService,
            IAttributeUtilities attributeUtilities,
            IRepository<CustomerShopMapping> customerShopMappingRepository,
            IBackendStockService backendStockService,
            IProductAbcDescriptionService productAbcDescriptionService
        ) : base(captchaSettings, customerSettings, checkoutAttributeParser, checkoutAttributeService,
            currencyService, customerActivityService, customerService, discountService,
            downloadService, genericAttributeService, giftCardService, localizationService,
            fileProvider, notificationService, permissionService, pictureService,
            priceFormatter, productAttributeParser, productAttributeService, productService,
            shippingService, shoppingCartModelFactory, shoppingCartService, staticCacheManager,
            storeContext, taxService, urlRecordService, webHelper,
            workContext, workflowMessageService, mediaSettings, orderSettings,
            shoppingCartSettings, shippingSettings)
        {
            _customerActivityService = customerActivityService;
            _localizationService = localizationService;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _shoppingCartService = shoppingCartService;
            _shoppingCartSettings = shoppingCartSettings;
            _storeContext = storeContext;
            _urlRecordService = urlRecordService;
            _workContext = workContext;

            _addToCartSlideoutService = addToCartSlideoutService;
            _attributeUtilities = attributeUtilities;
            _customerShopMappingRepository = customerShopMappingRepository;
            _backendStockService = backendStockService;
            _productAbcDescriptionService = productAbcDescriptionService;
        }

        //add product to cart using AJAX
        //currently we use this method on catalog pages (category/manufacturer/etc)

        // customized to check and add home delivery option by default
        // if it exists on the product
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public override async Task<IActionResult> AddProductToCart_Catalog(int productId, int shoppingCartTypeId,
            int quantity, bool forceredirection = false)
        {
            var cartType = (ShoppingCartType)shoppingCartTypeId;

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                //no product found
                return Json(new
                {
                    success = false,
                    message = "No product found with the specified ID"
                });

            //we can add only simple products
            if (product.ProductType != ProductType.SimpleProduct)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = await _urlRecordService.GetSeNameAsync(product) })
                });
            }

            //products with "minimum order quantity" more than a specified qty
            if (product.OrderMinimumQuantity > quantity)
            {
                //we cannot add to the cart such products from category pages
                //it can confuse customers. That's why we redirect customers to the product details page
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = await _urlRecordService.GetSeNameAsync(product) })
                });
            }

            if (product.CustomerEntersPrice)
            {
                //cannot be added to the cart (requires a customer to enter price)
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = await _urlRecordService.GetSeNameAsync(product) })
                });
            }

            if (product.IsRental)
            {
                //rental products require start/end dates to be entered
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = await _urlRecordService.GetSeNameAsync(product) })
                });
            }

            var allowedQuantities = _productService.ParseAllowedQuantities(product);
            if (allowedQuantities.Length > 0)
            {
                //cannot be added to the cart (requires a customer to select a quantity from dropdownlist)
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = await _urlRecordService.GetSeNameAsync(product) })
                });
            }

            string attributes = "";
            ProductAttributeMapping hdProductAttribute = null;

            var pams =  await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);

            foreach (var pam in pams)
            {
                var pa = await _productAttributeService.GetProductAttributeByIdAsync(pam.ProductAttributeId);

                switch (pa.Name)
                {
                    case "Home Delivery":
                        hdProductAttribute = pam;
                        break;
                }
            }

            // home delivery is default, so if it is home delivered, add the attribute no matter what
            if (hdProductAttribute != null)
            {
                attributes = await _attributeUtilities.InsertHomeDeliveryAttributeAsync(product, attributes);
            }

            //-------------------------------------END CUSTOM CODE------------------------------------------

            //get standard warnings without attribute validations
            //first, try to find existing shopping cart item
            var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), cartType, (await _storeContext.GetCurrentStoreAsync()).Id);

            //-----------------------------MODIFIED THIS LINE to add "attributes-----------------------------
            var shoppingCartItem = await _shoppingCartService.FindShoppingCartItemInTheCartAsync(cart, cartType, product, attributes);

            //if we already have the same product in the cart, then use the total quantity to validate
            var quantityToValidate = shoppingCartItem != null ? shoppingCartItem.Quantity + quantity : quantity;
            var addToCartWarnings = await _shoppingCartService
                .GetShoppingCartItemWarningsAsync(await _workContext.GetCurrentCustomerAsync(), cartType,
                product, (await _storeContext.GetCurrentStoreAsync()).Id, string.Empty,
                decimal.Zero, null, null, quantityToValidate, false, shoppingCartItem?.Id ?? 0, true, false, false, false);
            if (addToCartWarnings.Any())
            {
                //cannot be added to the cart
                //let's display standard warnings
                return Json(new
                {
                    success = false,
                    message = addToCartWarnings.ToArray()
                });
            }

            // ---------------------------------MODIFIED THIS LINE TO ADD ATTRIBUTES------------------------------
            //now let's try adding product to the cart (now including product attribute validation, etc)
            addToCartWarnings = await _shoppingCartService.AddToCartAsync(customer: await _workContext.GetCurrentCustomerAsync(),
                product: product,
                shoppingCartType: cartType,
                storeId: (await _storeContext.GetCurrentStoreAsync()).Id,
                quantity: quantity,
                attributesXml: attributes);
            if (addToCartWarnings.Any())
            {
                //cannot be added to the cart
                //but we do not display attribute and gift card warnings here. let's do it on the product details page
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = await _urlRecordService.GetSeNameAsync(product) })
                });
            }

            //added to the cart/wishlist
            switch (cartType)
            {
                case ShoppingCartType.Wishlist:
                    {
                        //activity log
                        await _customerActivityService.InsertActivityAsync("PublicStore.AddToWishlist",
                            string.Format(await _localizationService.GetResourceAsync( "ActivityLog.PublicStore.AddToWishlist"), product.Name), product);

                        if (_shoppingCartSettings.DisplayWishlistAfterAddingProduct || forceredirection)
                        {
                            //redirect to the wishlist page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("Wishlist")
                            });
                        }

                        //display notification message and update appropriate blocks
                        var shoppingCarts = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.Wishlist, (await _storeContext.GetCurrentStoreAsync()).Id);

                        var updatetopwishlistsectionhtml = string.Format(await _localizationService.GetResourceAsync( "Wishlist.HeaderQuantity"),
                            shoppingCarts.Sum(item => item.Quantity));
                        return Json(new
                        {
                            success = true,
                            message = string.Format(await _localizationService.GetResourceAsync( "Products.ProductHasBeenAddedToTheWishlist.Link"), Url.RouteUrl("Wishlist")),
                            updatetopwishlistsectionhtml
                        });
                    }

                case ShoppingCartType.ShoppingCart:
                default:
                    {
                        //activity log
                        await _customerActivityService.InsertActivityAsync("PublicStore.AddToShoppingCart",
                            string.Format(await _localizationService.GetResourceAsync( "ActivityLog.PublicStore.AddToShoppingCart"), product.Name), product);

                        if (_shoppingCartSettings.DisplayCartAfterAddingProduct || forceredirection)
                        {
                            //redirect to the shopping cart page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("ShoppingCart")
                            });
                        }

                        //display notification message and update appropriate blocks
                        var shoppingCarts = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);

                        var updatetopcartsectionhtml = string.Format(await _localizationService.GetResourceAsync( "ShoppingCart.HeaderQuantity"),
                            shoppingCarts.Sum(item => item.Quantity));

                        var updateflyoutcartsectionhtml = _shoppingCartSettings.MiniShoppingCartEnabled
                            ? await RenderViewComponentToStringAsync("FlyoutShoppingCart")
                            : string.Empty;

                        // ABC: Custom code for add to cart slideout
                        var addToCartSlideoutProductInfoHtml = await RenderViewComponentToStringAsync("AddToCartSlideoutProductInfo", new {productId = product.Id} );
                        var addToCartSlideoutSubtotalHtml = await RenderViewComponentToStringAsync("AddToCartSlideoutSubtotal", new {price = product.Price} );
                        var addToCartSlideoutProductAttributes = await RenderViewComponentToStringAsync("AddToCartSlideoutProductAttributes", new {product = product} );

                        return Json(new
                        {
                            success = true,
                            message = string.Format(await _localizationService.GetResourceAsync( "Products.ProductHasBeenAddedToTheCart.Link"), Url.RouteUrl("ShoppingCart")),
                            updatetopcartsectionhtml,
                            updateflyoutcartsectionhtml,
                            // ABC: custom response values
                            addToCartSlideoutProductInfoHtml,
                            // this will be the delivery zip
                            addToCartSlideoutProductAttributes,
                            addToCartSlideoutSubtotalHtml
                        });
                    }
            }
        }

        //add product to cart using AJAX
        //currently we use this method on the product details pages
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public override async Task<IActionResult> AddProductToCart_Details(int productId, int shoppingCartTypeId, IFormCollection form)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("Homepage")
                });
            }

            //we can add only simple products
            if (product.ProductType != ProductType.SimpleProduct)
            {
                return Json(new
                {
                    success = false,
                    message = "Only simple products could be added to the cart"
                });
            }

            //update existing shopping cart item
            var updatecartitemid = 0;
            foreach (var formKey in form.Keys)
                if (formKey.Equals($"addtocart_{productId}.UpdatedShoppingCartItemId", StringComparison.InvariantCultureIgnoreCase))
                {
                    int.TryParse(form[formKey], out updatecartitemid);
                    break;
                }

            ShoppingCartItem updatecartitem = null;
            if (_shoppingCartSettings.AllowCartItemEditing && updatecartitemid > 0)
            {
                //search with the same cart type as specified
                var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), (ShoppingCartType)shoppingCartTypeId, (await _storeContext.GetCurrentStoreAsync()).Id);

                updatecartitem = cart.FirstOrDefault(x => x.Id == updatecartitemid);
                //not found? let's ignore it. in this case we'll add a new item
                //if (updatecartitem == null)
                //{
                //    return Json(new
                //    {
                //        success = false,
                //        message = "No shopping cart item found to update"
                //    });
                //}
                //is it this product?
                if (updatecartitem != null && product.Id != updatecartitem.ProductId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "This product does not match a passed shopping cart item identifier"
                    });
                }
            }

            var addToCartWarnings = new List<string>();

            //customer entered price
            var customerEnteredPriceConverted = await _productAttributeParser.ParseCustomerEnteredPriceAsync(product, form);

            //entered quantity
            var quantity = _productAttributeParser.ParseEnteredQuantity(product, form);

            //product and gift card attributes
            var attributes = await _productAttributeParser.ParseProductAttributesAsync(product, form, addToCartWarnings);

            // --------------------------------
            // ABC: add Home Delivery attribute
            // --------------------------------
            attributes = await _attributeUtilities.InsertHomeDeliveryAttributeAsync(product, attributes);

            //rental attributes 
            _productAttributeParser.ParseRentalDates(product, form, out var rentalStartDate, out var rentalEndDate);

            var cartType = updatecartitem == null ? (ShoppingCartType)shoppingCartTypeId :
                //if the item to update is found, then we ignore the specified "shoppingCartTypeId" parameter
                updatecartitem.ShoppingCartType;

            await SaveItemAsync(updatecartitem, addToCartWarnings, product, cartType, attributes, customerEnteredPriceConverted, rentalStartDate, rentalEndDate, quantity);

            //return result
            return await GetProductToCartDetails(addToCartWarnings, cartType, product);
        }
    }
}