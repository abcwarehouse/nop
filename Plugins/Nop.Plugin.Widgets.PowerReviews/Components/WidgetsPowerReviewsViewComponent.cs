using Nop.Services.Logging;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Plugin.Misc.AbcCore.Services;
using System;
using Nop.Plugin.Widgets.PowerReviews.Models;
using System.Linq;
using Nop.Services.Catalog;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcFrontend.Extensions;
using Nop.Plugin.Misc.AbcCore.Infrastructure;
using Nop.Services.Common;
using Nop.Web.Models.Catalog;
using Nop.Core;

namespace Nop.Plugin.Widgets.PowerReviews.Components
{
    public class WidgetsPowerReviewsViewComponent : NopViewComponent
    {
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PowerReviewsSettings _settings;

        private readonly FrontEndService _frontEndService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IProductService _productService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly ICategoryService _categoryService;

        public WidgetsPowerReviewsViewComponent(
            ILogger logger,
            IWebHelper webHelper,
            PowerReviewsSettings settings,
            FrontEndService frontEndService,
            IGenericAttributeService genericAttributeService,
            IProductService productService,
            IProductAbcDescriptionService productAbcDescriptionService,
            ICategoryService categoryService
        )
        {
            _logger = logger;
            _webHelper = webHelper;
            _settings = settings;
            _frontEndService = frontEndService;
            _genericAttributeService = genericAttributeService;
            _productService = productService;
            _productAbcDescriptionService = productAbcDescriptionService;
            _categoryService = categoryService;
        }

        public IViewComponentResult Invoke(string widgetZone, object additionalData = null)
        {
            if (!_settings.IsValid())
            {
                _logger.Error("PowerReviews settings are required to have " +
                              "reviews display for products, add the correct " +
                              "settings in configuration.");
                return Content("");
            }

            if (widgetZone == CustomPublicWidgetZones.ProductBoxAddinfoReviews &&
                additionalData is ProductOverviewModel)
            {
                return Listing(additionalData as ProductOverviewModel);
            }
            if (widgetZone == CustomPublicWidgetZones.ProductDetailsReviews &&
                additionalData is ProductDetailsModel)
            {
                return View("~/Plugins/Widgets.PowerReviews/Views/Detail.cshtml");
            }
            if (widgetZone == CustomPublicWidgetZones.ProductDetailsReviewsTab)
            {
                return View("~/Plugins/Widgets.PowerReviews/Views/DetailTab.cshtml");
            }
            if (widgetZone == CustomPublicWidgetZones.ProductDetailsReviewsTabContent)
            {
                return View("~/Plugins/Widgets.PowerReviews/Views/DetailTabContent.cshtml");
            }
            if (widgetZone == PublicWidgetZones.CategoryDetailsBottom ||
                widgetZone == PublicWidgetZones.ManufacturerDetailsBottom)
            {
                return View("~/Plugins/Widgets.PowerReviews/Views/ListingScript.cshtml", _settings);
            }
            if (widgetZone == PublicWidgetZones.ProductDetailsBottom)
            {
                return Detail(additionalData as ProductDetailsModel);
            }

            _logger.Error($"Widgets.PowerReviews: No view provided for widget zone {widgetZone}");
            return Content("");
        }

        private IViewComponentResult Listing(ProductOverviewModel productOverviewModel)
        {
            var model = new ListingModel()
            {
                ProductId = productOverviewModel.Id,
                ProductSku = GetPowerReviewsSku(productOverviewModel.Sku, productOverviewModel.Id),
                Settings = _settings
            };

            return View(
                "~/Plugins/Widgets.PowerReviews/Views/Listing.cshtml",
                model
            );
        }

        private IViewComponentResult Detail(ProductDetailsModel productDetailsModel)
        {
            var manufacturerName = "";
            var manufacturerModel = productDetailsModel.ProductManufacturers.FirstOrDefault();
            if (manufacturerModel != null)
            {
                manufacturerName = manufacturerModel.Name;
            }

            var product = _productService.GetProductById(productDetailsModel.Id);
            var productAbcDescription = _productAbcDescriptionService.GetProductAbcDescriptionByProductId(product.Id);
            var productCategory = _categoryService.GetProductCategoriesByProductId(product.Id, true).FirstOrDefault();
            var category = _categoryService.GetCategoryById(productCategory.CategoryId);
            var specialPriceEndDate = product.GetSpecialPriceEndDate();
            var priceEndDate = specialPriceEndDate.HasValue ?
                specialPriceEndDate.Value.ToLocalTime() :
                DateTime.Now;

            var feedlessModel = new FeedlessProductModel()
            {
                Name = product.Name,
                Url = _webHelper.GetThisPageUrl(true),
                ImageUrl = productDetailsModel.DefaultPictureModel.ImageUrl,
                Description = productAbcDescription?.AbcDescription ?? product.ShortDescription,
                CategoryName = category.Name,
                ManufacturerId = manufacturerModel.Id,
                Upc = product.Gtin,
                BrandName = manufacturerName,
                InStock = !product.DisableBuyButton
            };

            var model = new DetailModel()
            {
                ProductSku = GetPowerReviewsSku(productDetailsModel.Sku, productDetailsModel.Id),
                ProductId = productDetailsModel.Id,
                ProductPrice = productDetailsModel.ProductPrice.PriceValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                PriceValidUntil = priceEndDate,
                Settings = _settings,
                FeedlessProduct = feedlessModel
            };

            return View(
                "~/Plugins/Widgets.PowerReviews/Views/DetailScript.cshtml",
                model
            );
        }

        // PowerReviews requires a SKU with only letters, numbers, and -
        // this function also considers the ABC package product
        private string GetPowerReviewsSku(string sku, int productId)
        {
            if (string.IsNullOrWhiteSpace(sku)) return "";

            var packageProduct = _frontEndService.GetProductIdInPackage(sku);
            var powerReviewsPageId = packageProduct != null && packageProduct.Product_Id != 0 ?
                                        packageProduct.Sku :
                                        sku;

            var mattressSku = _genericAttributeService.GetAttributesForEntity(productId, "Product")
                                                      .FirstOrDefault(a => a.Key == "MattressSku");
            if (!string.IsNullOrWhiteSpace(mattressSku?.Value))
            {
                powerReviewsPageId = mattressSku.Value;
            }

            char[] conversionString = powerReviewsPageId.ToCharArray();
            conversionString = Array.FindAll<char>(conversionString, (c => (char.IsLetterOrDigit(c)
                                    || c == '-')));
            return new string(conversionString);
        }
    }
}
