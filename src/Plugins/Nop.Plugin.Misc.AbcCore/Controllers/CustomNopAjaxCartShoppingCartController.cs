using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AbcCore.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using SevenSpikes.Nop.Plugins.AjaxCart.Domain;
using SevenSpikes.Nop.Plugins.AjaxCart.Controllers;
using Nop.Core.Domain.Shipping;
using Nop.Services.Shipping;
using Nop.Services.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Tax;
using Nop.Core.Domain.Media;
using Nop.Core;
using Nop.Services.Seo;
using Nop.Core.Caching;
using Nop.Services.Orders;
using Nop.Web.Factories;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Common;
using Nop.Core.Infrastructure;
using Nop.Services.Discounts;
using Nop.Services.Customers;
using Nop.Services.Logging;
using Nop.Services.Directory;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Microsoft.AspNetCore.Http;

namespace Nop.Plugin.Misc.AbcCore.Controllers
{
    public class CustomNopAjaxCartShoppingCartController : NopAjaxCartShoppingCartController
    {
        public CustomNopAjaxCartShoppingCartController(
            NopAjaxCartSettings ajaxCartSettings,
            IProductAttributeFormatter productAttributeFormatter,
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
            INopFileProvider nopFileProvider,
            INotificationService notificationService,
            IPermissionService permissionService,
            IPictureService pictureService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
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
            IShippingService shippingService,
            ShippingSettings shippingSettings
        ) : base(ajaxCartSettings,
            productAttributeFormatter,
            captchaSettings,
            customerSettings,
            checkoutAttributeParser,
            checkoutAttributeService,
            currencyService,
            customerActivityService,
            customerService,
            discountService,
            downloadService,
            genericAttributeService,
            giftCardService,
            localizationService,
            nopFileProvider,
            notificationService,
            permissionService,
            pictureService,
            priceFormatter,
            productAttributeParser,
            productAttributeService,
            productService,
            shoppingCartModelFactory,
            shoppingCartService,
            staticCacheManager,
            storeContext,
            taxService,
            urlRecordService,
            webHelper,
            workContext,
            workflowMessageService,
            mediaSettings,
            orderSettings,
            shoppingCartSettings,
            shippingService,
            shippingSettings)
        {
        }

        // public override Task<IActionResult> AddProductFromProductDetailsPageToCartAjax(int productId, bool isAddToCartButton, IFormCollection form)
        // {
        //     return await base.AddProductFromProductDetailsPageToCartAjax(productId, isAddToCartButton, form);
        // }
    }
}
