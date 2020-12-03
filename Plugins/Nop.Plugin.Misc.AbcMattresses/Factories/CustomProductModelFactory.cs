using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Nop.Services.Caching;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping.Date;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;

// Checks for specific mattress listing prices
namespace Nop.Plugin.Misc.AbcMattresses.Factories
{
    /// <summary>
    /// Represents the product model factory
    /// </summary>
    public partial class CustomProductModelFactory : ProductModelFactory, IProductModelFactory
    {
        private readonly IWebHelper _webHelper;
        private readonly IAbcMattressListingPriceService _abcMattressListingPriceService;
        public CustomProductModelFactory(
            CaptchaSettings captchaSettings,
            CatalogSettings catalogSettings,
            CustomerSettings customerSettings,
            ICacheKeyService cacheKeyService,
            ICategoryService categoryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IDateRangeService dateRangeService,
            IDateTimeHelper dateTimeHelper,
            IDownloadService downloadService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            IPermissionService permissionService,
            IPictureService pictureService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IProductTagService productTagService,
            IProductTemplateService productTemplateService,
            IReviewTypeService reviewTypeService,
            ISpecificationAttributeService specificationAttributeService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IShoppingCartModelFactory shoppingCartModelFactory,
            ITaxService taxService,
            IUrlRecordService urlRecordService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            OrderSettings orderSettings,
            SeoSettings seoSettings,
            ShippingSettings shippingSettings,
            VendorSettings vendorSettings,
            IAbcMattressListingPriceService abcMattressListingPriceService)
            : base(captchaSettings, catalogSettings, customerSettings, cacheKeyService,
                categoryService, currencyService, customerService, dateRangeService,
                dateTimeHelper, downloadService, genericAttributeService, localizationService,
                manufacturerService, permissionService, pictureService, priceCalculationService,
                priceFormatter, productAttributeParser, productAttributeService, productService,
                productTagService, productTemplateService, reviewTypeService, specificationAttributeService,
                staticCacheManager, storeContext, shoppingCartModelFactory, taxService, urlRecordService,
                vendorService, webHelper, workContext, mediaSettings, orderSettings, seoSettings,
                shippingSettings, vendorSettings
            )
        {
            _webHelper = webHelper;
            _abcMattressListingPriceService = abcMattressListingPriceService;
        }

        /// <summary>
        /// Prepare the product overview price model
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="forceRedirectionAfterAddingToCart">Whether to force redirection after adding to cart</param>
        /// <returns>Product overview price model</returns>
        protected override ProductOverviewModel.ProductPriceModel PrepareProductOverviewPriceModel(
            Product product,
            bool forceRedirectionAfterAddingToCart = false
        )
        {
            var model = base.PrepareProductOverviewPriceModel(product, forceRedirectionAfterAddingToCart);

            var url = _webHelper.GetThisPageUrl(false);
            var newPrice = _abcMattressListingPriceService.GetListingPriceForMattressProduct(
                product.Id,
                url
            );

            if (newPrice != null)
            {
                model.Price = $"${newPrice}";
            }

            return model;
        }
    }
}