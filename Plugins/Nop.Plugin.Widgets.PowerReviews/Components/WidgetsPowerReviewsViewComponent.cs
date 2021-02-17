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

namespace Nop.Plugin.Widgets.PowerReviews.Components
{
    public class WidgetsPowerReviewsViewComponent : NopViewComponent
    {
        private readonly ILogger _logger;

        private readonly FrontEndService _frontEndService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IProductService _productService;

        public WidgetsPowerReviewsViewComponent(
            ILogger logger,
            FrontEndService frontEndService,
            IGenericAttributeService genericAttributeService,
            IProductService productService
        )
        {
            _logger = logger;
            _frontEndService = frontEndService;
            _genericAttributeService = genericAttributeService;
            _productService = productService;
        }

        public IViewComponentResult Invoke(string widgetZone, object additionalData = null)
        {
            if (widgetZone == CustomPublicWidgetZones.ProductBoxAddinfoReviews &&
                additionalData is ProductOverviewModel)
            {
                return Listing(additionalData as ProductOverviewModel);
            }
            if (widgetZone == CustomPublicWidgetZones.ProductDetailsReviews &&
                additionalData is ProductDetailsModel)
            {
                return Detail(additionalData as ProductDetailsModel);
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
                return View("~/Plugins/Widgets.PowerReviews/Views/ListingScript.cshtml");
            }

            _logger.Error($"Widgets.PowerReviews: No view provided for widget zone {widgetZone}");
            return Content("");
        }

        private IViewComponentResult Listing(ProductOverviewModel productOverviewModel)
        {
            var model = new ListingModel()
            {
                ProductId = productOverviewModel.Id,
                ProductSku = GetPowerReviewsSku(productOverviewModel.Sku, productOverviewModel.Id)
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
            var priceEndDate = DateTime.Now;
            var specialPriceEndDate = product.GetSpecialPriceEndDate();
            if (specialPriceEndDate.HasValue && !product.IsAbcGiftCard())
            {
                priceEndDate = specialPriceEndDate.Value.ToLocalTime();
            }

            var model = new DetailModel()
            {
                ManufacturerName = manufacturerName,
                ProductSku = GetPowerReviewsSku(productDetailsModel.Sku, productDetailsModel.Id),
                PriceValidUntil = priceEndDate,
                ProductName = productDetailsModel.Name,
                MetaDescription = productDetailsModel.MetaDescription,
                ProductImageUrl = productDetailsModel.DefaultPictureModel.ImageUrl,
                ProductGtin = productDetailsModel.Gtin,
                ProductPrice = productDetailsModel.ProductPrice.PriceValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            };

            return View(
                "~/Plugins/Widgets.PowerReviews/Views/Detail.cshtml",
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
