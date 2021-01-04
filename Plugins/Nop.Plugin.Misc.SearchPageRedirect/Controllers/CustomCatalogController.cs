using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Services.Configuration;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Models.Catalog;
using Nop.Web.Factories;

namespace Nop.Plugin.Misc.SearchPageRedirect.Controllers
{
    public class CustomCatalogController : Web.Controllers.CatalogController
    {
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;

        public CustomCatalogController(
            CatalogSettings catalogSettings,
            IAclService aclService,
            ICatalogModelFactory catalogModelFactory,
            ICategoryService categoryService,
            ICustomerActivityService customerActivityService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            IPermissionService permissionService,
            IProductModelFactory productModelFactory,
            IProductService productService,
            IProductTagService productTagService,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            VendorSettings vendorSettings,
            ILogger logger,
            ISettingService settingService
        ) : base(catalogSettings, aclService, catalogModelFactory, categoryService,
            customerActivityService, genericAttributeService, localizationService,
            manufacturerService, permissionService, productModelFactory, productService,
            productTagService, storeContext, storeMappingService, vendorService, webHelper,
            workContext, mediaSettings, vendorSettings)
        {
            _logger = logger;
            _settingService = settingService;
        }

        public override IActionResult Search(SearchModel model, CatalogPagingFilteringModel command)
        {
            var redirectUrl = _settingService.GetSetting("searchpageredirect.url")?.Value;

            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                _logger.Warning("No search page redirect URL is provided, defaulting to normal search page behavior.");
                return base.Search(model, command);
            }

            return Redirect(redirectUrl);
        }
    }
}
