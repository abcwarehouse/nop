using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcCore.Infrastructure;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Widgets.AbcPromos.Models;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Widgets.AbcPromos.Components
{
    public class WidgetsAbcPromosViewComponent : NopViewComponent
    {
        private readonly ILogger _logger;
        private readonly IAbcPromoService _abcPromoService;

        private readonly string DirectoryName = "promo_banners";
        private readonly string DirectoryPath;

        public WidgetsAbcPromosViewComponent(
            ILogger logger,
            IAbcPromoService abcPromoService
        )
        {
            _logger = logger;
            _abcPromoService = abcPromoService;

            DirectoryPath = $"{CoreUtilities.WebRootPath()}/{DirectoryName}";
        }
        public IViewComponentResult Invoke(string widgetZone, object additionalData = null)
        {
            if (widgetZone == CustomPublicWidgetZones.ProductBoxAddinfoReviews)
            {
                return ProductBoxPromo(additionalData);
            }

            if (widgetZone == CustomPublicWidgetZones.ProductDetailsAfterPrice ||
                widgetZone == CustomPublicWidgetZones.OrderSummaryAfterProductMiniDescription)
            {
                return ProductDetailPromos(Int32.Parse(additionalData.ToString()));
            }

            if (widgetZone == PublicWidgetZones.ProductDetailsAfterBreadcrumb)
            {
                return DisplayBanner(additionalData);
            }

            _logger.Error($"Widgets.AbcPromos: Invalid Widget Zone passed ({widgetZone}).");
            return Content("");
        }

        private IViewComponentResult ProductBoxPromo(object additionalData)
        {
            if (additionalData == null || !(additionalData is ProductOverviewModel))
            {
                _logger.Error("ProductOverviewModel not passed to Widgets.AbcPromos - skipping display of product box promo.");
                return Content("");
            }

            var productId = (additionalData as ProductOverviewModel).Id;
            var promos = _abcPromoService.GetActivePromosByProductId(productId).Take(2);

            var promosArray = new AbcPromo[]
            {
                promos.Count() > 0 ? promos.ElementAt(0) : null,
                promos.Count() > 1 ? promos.ElementAt(1) : null,
            };

            return View("~/Plugins/Widgets.AbcPromos/Views/ProductBoxPromos.cshtml", promosArray);
        }

        private IViewComponentResult ProductDetailPromos(int productId)
        {
            var promos = _abcPromoService.GetActivePromosByProductId(productId);

            return View("~/Plugins/Widgets.AbcPromos/Views/ProductDetailPromos.cshtml", promos);
        }

        private IViewComponentResult DisplayBanner(object additionalData)
        {
            if (additionalData == null || !(additionalData is ProductDetailsModel))
            {
                _logger.Error("ProductDetailsModel not passed to Widgets.AbcPromos - skipping display of promo banner.");
                return Content("");
            }

            int productId = (additionalData as ProductDetailsModel).Id;
            var promos = _abcPromoService.GetActivePromosByProductId(productId);
            if (!promos.Any()) { return Content(""); }

            InitializePromoBannersFolder();

            var banners = new List<BannerModel>();
            foreach (var promo in promos)
            {
                var bannerImage = promo.GetPromoBannerUrl();

                if (bannerImage != null)
                {
                    var bannerModel = new BannerModel
                    {
                        AltText = promo.Name,
                        BannerImageUrl = bannerImage,
                        PromoFormPopup = promo.GetPopupCommand()
                    };
                    banners.Add(bannerModel);
                }
            }

            return View("~/Plugins/Widgets.AbcPromos/Views/DisplayBanner.cshtml", banners);
        }

        private void InitializePromoBannersFolder()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                _logger.Information("Widgets.AbcPromos: \"promo_banners\" directory created, as it did not exist.");
                Directory.CreateDirectory(DirectoryPath);
            }
        }
    }
}
