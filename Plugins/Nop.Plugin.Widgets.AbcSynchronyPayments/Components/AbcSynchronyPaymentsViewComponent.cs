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
using Nop.Services.Common;
using System.Linq;
using Nop.Plugin.Misc.AbcMattresses.Services;

namespace Nop.Plugin.Widgets.AbcSynchronyPayments.Components
{
    [ViewComponent(Name = "AbcSynchronyPayments")]
    public class AbcSynchronyPaymentsViewComponent : NopViewComponent
    {
        private readonly ILogger _logger;
        private readonly IAbcMattressListingPriceService _abcMattressListingPriceService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IProductAbcFinanceService _productAbcFinanceService;
        private readonly IProductService _productService;
        private readonly IStoreContext _storeContext;

        public AbcSynchronyPaymentsViewComponent(
            ILogger logger,
            IGenericAttributeService genericAttributeService,
            IAbcMattressListingPriceService abcMattressListingPriceService,
            IProductAbcDescriptionService productAbcDescriptionService,
            IProductAbcFinanceService productAbcFinanceService,
            IProductService productService,
            IStoreContext storeContext
        )
        {
            _logger = logger;
            _abcMattressListingPriceService = abcMattressListingPriceService;
            _genericAttributeService = genericAttributeService;
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

            // also allow getting info from generic attribute
            var productGenericAttributes = _genericAttributeService.GetAttributesForEntity(productId, "Product");
            var monthsGenericAttribute = productGenericAttributes.Where(ga => ga.Key == "SynchronyPaymentMonths")
                                                                 .FirstOrDefault();

            if (abcItemNumber == null && monthsGenericAttribute == null)
            {
                // No ABC Item number (or no months indicator), skip processing
                return View(productListingCshtml, model);
            }

            var productAbcFinance =
                _productAbcFinanceService.GetProductAbcFinanceByAbcItemNumber(
                    abcItemNumber
                );
            if (productAbcFinance == null && monthsGenericAttribute == null)
            {
                // No financing information
                return View(productListingCshtml, model);
            }

            // generic attribute data
            var isMinimumGenericAttribute = productGenericAttributes.Where(ga => ga.Key == "SynchronyPaymentIsMinimum")
                                                                 .FirstOrDefault();
            var offerValidFromGenericAttribute = productGenericAttributes.Where(ga => ga.Key == "SynchronyPaymentOfferValidFrom")
                                                                 .FirstOrDefault();
            var offerValidToGenericAttribute = productGenericAttributes.Where(ga => ga.Key == "SynchronyPaymentOfferValidTo")
                                                                 .FirstOrDefault();

            var product = _productService.GetProductById(productId);
            var months = productAbcFinance != null ?
                productAbcFinance.Months :
                int.Parse(monthsGenericAttribute.Value);
            var isMinimumPayment = productAbcFinance != null ?
                productAbcFinance.IsDeferredPricing :
                bool.Parse(isMinimumGenericAttribute.Value);
            var offerValidFrom = productAbcFinance != null ?
                productAbcFinance.StartDate.Value :
                DateTime.Parse(offerValidFromGenericAttribute.Value);
            var offerValidTo = productAbcFinance != null ?
                productAbcFinance.EndDate.Value :
                DateTime.Parse(offerValidToGenericAttribute.Value);
            var price = _abcMattressListingPriceService.GetListingPriceForMattressProduct(productId) ?? product.Price;
            
            model = new SynchronyPaymentModel
            {
                MonthCount = months,
                MonthlyPayment = CalculatePayment(
                    price, isMinimumPayment, months
                ),
                ProductId = productId,
                ApplyUrl = GetApplyUrl(),
                IsMonthlyPaymentStyle = !isMinimumPayment,
                EqualPayment = CalculateEqualPayments(
                    price, months
                ),
                ModalHexColor = HtmlHelpers.GetPavilionPrimaryColor(),
                StoreName = _storeContext.CurrentStore.Name,
                ImageUrl = GetImageUrl(),
                OfferValidFrom = offerValidFrom.ToShortDateString(),
                OfferValidTo = offerValidTo.ToShortDateString()
            };

            model.FullPrice = price;
            model.FinalPayment = model.FullPrice -
                (model.MonthlyPayment * (model.MonthCount - 1));

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

        private int CalculatePayment(decimal productPrice, bool isMinimumPayment, int months)
        {
            return isMinimumPayment ?
                (int)Math.Max(Math.Round(Math.Ceiling(productPrice * 0.035M), 2), 28) :
                CalculateEqualPayments(productPrice, months);
        }

        private int CalculateEqualPayments(decimal productPrice, int months)
        {
            return (int)Math.Round(Math.Ceiling(productPrice / months), 2);
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
