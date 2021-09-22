using System;
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

namespace Nop.Plugin.Misc.AbcFrontend.Controllers
{
    public partial class CustomShoppingCartController : BasePublicController
    {
        private readonly CaptchaSettings _captchaSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICheckoutAttributeService _checkoutAttributeService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly IDownloadService _downloadService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGiftCardService _giftCardService;
        private readonly ILocalizationService _localizationService;
        private readonly INopFileProvider _fileProvider;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly IPictureService _pictureService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly MediaSettings _mediaSettings;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        // custom
        private readonly IAttributeUtilities _attributeUtilities;
        private readonly IRepository<CustomerShopMapping> _customerShopMappingRepository;
        private readonly IBackendStockService _backendStockService;

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
            IAttributeUtilities attributeUtilities,
            IRepository<CustomerShopMapping> customerShopMappingRepository,
            IBackendStockService backendStockService
        )
        {
            _captchaSettings = captchaSettings;
            _customerSettings = customerSettings;
            _checkoutAttributeParser = checkoutAttributeParser;
            _checkoutAttributeService = checkoutAttributeService;
            _currencyService = currencyService;
            _customerActivityService = customerActivityService;
            _customerService = customerService;
            _discountService = discountService;
            _downloadService = downloadService;
            _genericAttributeService = genericAttributeService;
            _giftCardService = giftCardService;
            _localizationService = localizationService;
            _fileProvider = fileProvider;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _pictureService = pictureService;
            _priceFormatter = priceFormatter;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _shippingService = shippingService;
            _shoppingCartModelFactory = shoppingCartModelFactory;
            _shoppingCartService = shoppingCartService;
            _staticCacheManager = staticCacheManager;
            _storeContext = storeContext;
            _taxService = taxService;
            _urlRecordService = urlRecordService;
            _webHelper = webHelper;
            _workContext = workContext;
            _workflowMessageService = workflowMessageService;
            _mediaSettings = mediaSettings;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;

            _attributeUtilities = attributeUtilities;
            _customerShopMappingRepository = customerShopMappingRepository;
            _backendStockService = backendStockService;
        }

        #region Shopping cart

        //add product to cart using AJAX
        //currently we use this method on the product details pages
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> AddProductToCart_Details(int productId, int shoppingCartTypeId, IFormCollection form)
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

            await SaveItem(updatecartitem, addToCartWarnings, product, cartType, attributes, customerEnteredPriceConverted, rentalStartDate, rentalEndDate, quantity);

            //return result
            return await GetProductToCartDetails(addToCartWarnings, cartType, product);
        }

        #endregion
    }
}