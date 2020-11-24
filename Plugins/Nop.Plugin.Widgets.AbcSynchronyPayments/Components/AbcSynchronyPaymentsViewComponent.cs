using System;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Infrastructure;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Widgets.AbcSynchronyPayments.Domain;
using Nop.Plugin.Widgets.AbcSynchronyPayments.Models;
using Nop.Plugin.Widgets.AbcSynchronyPayments.Services;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Widgets.AbcSynchronyPayments.Components
{
    [ViewComponent(Name = "AbcSynchronyPayments")]
    public class AbcSynchronyPaymentsViewComponent : NopViewComponent
    {
        private readonly ILogger _logger;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IProductAbcFinanceService _productAbcFinanceService;
        private readonly IProductService _productService;
        private readonly IStoreContext _storeContext;

        public AbcSynchronyPaymentsViewComponent(
            ILogger logger,
            IProductAbcDescriptionService productAbcDescriptionService,
            IProductAbcFinanceService productAbcFinanceService,
            IProductService productService,
            IStoreContext storeContext
        )
        {
            _logger = logger;
            _productAbcDescriptionService = productAbcDescriptionService;
            _productAbcFinanceService = productAbcFinanceService;
            _productService = productService;
            _storeContext = storeContext;
        }

        /*
         * Because of the way AJAX loading of products works, we should always
         * pass in a View instead of an EmptyResult. This ensures the CSS for
         * the modal is always loaded.
         */
        public IViewComponentResult Invoke(
            string widgetZone,
            object additionalData = null
        )
        {
            const string productListingCshtml =
                "~/Plugins/Widgets.AbcSynchronyPayments/Views/ProductListing.cshtml";
            SynchronyPaymentModel model = null;

            var productId = GetProductId(additionalData);
            if (productId == -1)
            {
                _logger.Error(
                    "Incorrect data model passed to ABC Warehouse " +
                    "Synchrony Payments widget.");
                return View(productListingCshtml, model);
            }

            var productAbcDescription = _productAbcDescriptionService.GetProductAbcDescriptionByProductId(productId);
            var abcItemNumber = productAbcDescription?.AbcItemNumber;
            if (abcItemNumber == null)
            {
                // No ABC Item number, skip processing
                return View(productListingCshtml, model);
            }

            var productAbcFinance =
                _productAbcFinanceService.GetProductAbcFinanceByAbcItemNumber(
                    abcItemNumber
                );
            if (productAbcFinance == null)
            {
                // No financing information
                return View(productListingCshtml, model);
            }

            var product = _productService.GetProductById(productId);
            model = new SynchronyPaymentModel
            {
                MonthCount = productAbcFinance.Months,
                MonthlyPayment = CalculatePayment(
                    product.Price, productAbcFinance
                ),
                ProductId = productId,
                ApplyUrl = GetApplyUrl(),
                IsMonthlyPaymentStyle = productAbcFinance.IsMonthlyPricing,
                EqualPayment = CalculateEqualPayments(
                    product.Price, productAbcFinance
                ),
                ModalHexColor = HtmlHelpers.GetPavilionPrimaryColor(),
                StoreName = _storeContext.CurrentStore.Name,
                ImageUrl = GetImageUrl(),
                OfferValidFrom = productAbcFinance.StartDate.HasValue ?
                    productAbcFinance.StartDate.Value.ToShortDateString() :
                    null,
                OfferValidTo = productAbcFinance.EndDate.HasValue ?
                    productAbcFinance.EndDate.Value.ToShortDateString() :
                    null,
            };

            if (model.MonthlyPayment == 0)
            {
                _logger.Warning(
                    $"ABC Product #{productAbcFinance.AbcItemNumber} has a " +
                    "$0 monthly fee, likely not marked with a correct " +
                    "payment type.");
                return View(productListingCshtml, model);
            }

            switch (widgetZone)
            {
                case PublicWidgetZones.ProductBoxAddinfoMiddle:
                    return View(productListingCshtml, model);
                case CustomPublicWidgetZones.ProductDetailsAfterPrice:
                    return View("~/Plugins/Widgets.AbcSynchronyPayments/Views/ProductDetail.cshtml", model);
            }

            _logger.Warning(
                "ABC Synchrony Payments: Did not match with any passed " +
                "widgets, skipping display");
            return View(productListingCshtml, model);
        }

        private string GetImageUrl()
        {
            return _storeContext.CurrentStore.Name == "ABC Warehouse" ?
                "/Plugins/Widgets.AbcSynchronyPayments/Images/deferredPricing.PNG" :
                "/Plugins/Widgets.AbcSynchronyPayments/Images/deferredPricing-haw.PNG";
        }

        private int CalculatePayment(decimal productPrice, ProductAbcFinance productAbcFinance)
        {
            if (productAbcFinance.IsDeferredPricing)
            {
                return (int)Math.Max(Math.Round(Math.Ceiling(productPrice * 0.035M), 2), 28);
            }
            else if (productAbcFinance.IsMonthlyPricing)
            {
                return CalculateEqualPayments(productPrice, productAbcFinance);
            }
            else
            {
                return 0;
            }
        }

        private int CalculateEqualPayments(decimal productPrice, ProductAbcFinance productAbcFinance)
        {
            return (int)Math.Round(Math.Ceiling(productPrice / productAbcFinance.Months), 2);
        }

        private string GetApplyUrl()
        {
            return _storeContext.CurrentStore.Name == "ABC Warehouse" ?
                "https://etail.mysynchrony.com/eapply/eapply.action?uniqueId=7EDBAEB071977CE89751663EC89BC474D77B435DDDADAB79&client=ABC%20Warehouse" :
                "https://etail.mysynchrony.com/eapply/eapply.action?uniqueId=7EDBAEB071977CE89751663EC89BC474D77B435DDDADAB79&client=Hawthorne";
        }

        // Gets product ID based on the model passed in
        private int GetProductId(object additionalData)
        {
            if (additionalData is ProductOverviewModel)
            {
                return (additionalData as ProductOverviewModel).Id;
            }
            if (additionalData.GetType() == typeof(int))
            {
                return (int)additionalData;
            }

            return -1;
        }
    }
}
