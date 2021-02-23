using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcPromos.Models;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;
using Nop.Services.Seo;
using Nop.Services.Logging;
using Nop.Services.Catalog;
using System.Collections.Generic;
using Nop.Plugin.Widgets.AbcPromos;

namespace Nop.Plugin.Misc.AbcPromos.Controllers
{
    public class CustomCatalogController : BasePublicController
    {
        private readonly IAbcPromoService _abcPromoService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IUrlRecordService _urlRecordService;

        private readonly IProductModelFactory _productModelFactory;
        private readonly ICatalogModelFactory _catalogModelFactory;

        private readonly ILogger _logger;

        private readonly AbcPromosSettings _settings;

        public CustomCatalogController(
            IAbcPromoService abcPromoService,
            IManufacturerService manufacturerService,
            IUrlRecordService urlRecordService,
            IProductModelFactory productModelFactory,
            ICatalogModelFactory categoryModelFactory,
            ILogger logger,
            AbcPromosSettings settings
        )
        {
            _abcPromoService = abcPromoService;
            _manufacturerService = manufacturerService;
            _urlRecordService = urlRecordService;
            _productModelFactory = productModelFactory;
            _catalogModelFactory = categoryModelFactory;
            _logger = logger;
            _settings = settings;
        }

        public IActionResult Promo(string promoSlug, CatalogPagingFilteringModel command)
        {
            var urlRecord = _urlRecordService.GetBySlug(promoSlug);
            if (urlRecord == null) return InvokeHttp404();

            var promo = _abcPromoService.GetPromoById(urlRecord.EntityId);
            if (promo == null) return InvokeHttp404();

            var shouldDisplay = _settings.IncludeExpiredPromosOnRebatesPromosPage ?
                promo.IsExpired() || promo.IsActive() :
                promo.IsActive();
            if (!shouldDisplay) return InvokeHttp404();

            var promoProducts = _abcPromoService.GetPublishedProductsByPromoId(promo.Id);
            promoProducts = SortPromoProducts(promoProducts, command);

            var filteredPromoProducts = promoProducts.Skip(command.PageIndex * 20).Take(20);

            var model = new PromoListingModel
            {
                Name = promo.ManufacturerId != null ?
                            $"{_manufacturerService.GetManufacturerById(promo.ManufacturerId.Value).Name} - {promo.Description}" :
                            promo.Description,
                Products = _productModelFactory.PrepareProductOverviewModels(filteredPromoProducts).ToList(),
                PagingFilteringContext = new CatalogPagingFilteringModel(),
                BannerImageUrl = promo.GetPromoBannerUrl(),
                PromoFormPopup = promo.GetPopupCommand()
            };

            var pagedList = new PagedList<Product>(
                filteredPromoProducts,
                command.PageIndex,
                20,
                promoProducts.Count
            );
            model.PagingFilteringContext.LoadPagedList(pagedList);

            _catalogModelFactory.PrepareSortingOptions(model.PagingFilteringContext, command);
            model.PagingFilteringContext.AvailableSortOptions =
                model.PagingFilteringContext.AvailableSortOptions.Where(aso => aso.Text.Contains("Price:")).ToList();

            return View("~/Plugins/Widgets.AbcPromos/Views/PromoListing.cshtml", model);
        }

        private List<Product> SortPromoProducts(
            IList<Product> promoProducts,
            CatalogPagingFilteringModel command
        )
        {
            if (command.OrderBy == 11)
            {
                return promoProducts.OrderByDescending(p => p.Price).ToList();
            }

            return promoProducts.OrderBy(p => p.Price).ToList();
        }
    }
}