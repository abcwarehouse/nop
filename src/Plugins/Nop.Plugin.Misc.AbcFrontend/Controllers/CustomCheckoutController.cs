using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Orders;
using System.Linq;
using Nop.Services.Catalog;
using System.Collections.Generic;
using Nop.Services.Payments;
using System;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Payments;
using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Services.Shipping;
using Nop.Services.Directory;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using Nop.Core.Domain.Catalog;
using System.Text;
using System.Xml;
using System.Configuration;
using Nop.Services.Configuration;
using System.Net;
using Nop.Plugin.Misc.AbcFrontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Nop.Web.Models.Checkout;
// needed for HttpContext
using Nop.Core.Http.Extensions;
// needed for .ToEntity()
using Nop.Web.Extensions;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcFrontend.Services;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcExportOrder.Services;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Controllers
{
    public class CustomCheckoutController : BasePublicController
    {
        private readonly AddressSettings _addressSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressService _addressService;
        private readonly ICheckoutModelFactory _checkoutModelFactory;
        private readonly ICountryService _countryService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ICustomOrderService _orderService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPaymentService _paymentService;
        private readonly IProductService _productService;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;

        private readonly OrderSettings _orderSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly CoreSettings _coreSettings;

        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductAttributeParser _productAttributeParser;

        private readonly ISettingService _settingService;
        private readonly IGiftCardService _giftCardService;
        private readonly IIsamGiftCardService _isamGiftCardService;
        private readonly IWarrantyService _warrantyService;
        private readonly ITermLookupService _termLookupService;
        private readonly ICardCheckService _cardCheckService;
        private readonly string DefaultTransPromo;

        public CustomCheckoutController(
            AddressSettings addressSettings,
            CustomerSettings customerSettings,
            IAddressAttributeParser addressAttributeParser,
            IAddressService addressService,
            ICheckoutModelFactory checkoutModelFactory,
            ICountryService countryService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            ICustomOrderService orderService,
            IPaymentPluginManager paymentPluginManager,
            IPaymentService paymentService,
            IProductService productService,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IWebHelper webHelper,
            IWorkContext workContext,
            OrderSettings orderSettings,
            PaymentSettings paymentSettings,
            RewardPointsSettings rewardPointsSettings,
            ShippingSettings shippingSettings,
            CoreSettings coreSettings,
            IProductAttributeService productAttributeService,
            IProductAttributeParser productAttributeParser,
            ISettingService settingService,
            IGiftCardService giftCardService,
            IIsamGiftCardService isamGiftCardService,
            IWarrantyService warrantyService,
            ITermLookupService termLookupService,
            ICardCheckService cardCheckService
        )
        {
            _addressSettings = addressSettings;
            _customerSettings = customerSettings;
            _addressAttributeParser = addressAttributeParser;
            _addressService = addressService;
            _checkoutModelFactory = checkoutModelFactory;
            _countryService = countryService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _logger = logger;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _paymentPluginManager = paymentPluginManager;
            _paymentService = paymentService;
            _productService = productService;
            _shippingService = shippingService;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _workContext = workContext;
            _orderSettings = orderSettings;
            _paymentSettings = paymentSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _shippingSettings = shippingSettings;
            _coreSettings = coreSettings;

            _productAttributeService = productAttributeService;
            _productAttributeParser = productAttributeParser;
            _settingService = settingService;
            _giftCardService = giftCardService;
            _isamGiftCardService = isamGiftCardService;
            _warrantyService = warrantyService;
            _termLookupService = termLookupService;
            _cardCheckService = cardCheckService;

            DefaultTransPromo = _settingService.GetSetting("ordersettings.defaulttranspromo")?.Value;
            if (string.IsNullOrWhiteSpace(DefaultTransPromo))
            {
                throw new ConfigurationErrorsException("'ordersettings.defaulttranspromo' setting is required for CustomCheckoutController.");
            }
        }

        #region Methods (one page checkout)
        private string SendExternalShippingMethodRequest()
        {
            if (DefaultTransPromo == "101")
            {
                _logger.Warning("DefaultTransPromo is 101, term lookup skipped");
                return string.Empty;
            }

            try
            {
                var cart = await _shoppingCartService.GetShoppingCartAsync(
                    await _workContext.GetCurrentCustomerAsync(),
                    ShoppingCartType.ShoppingCart,
                    (await _storeContext.GetCurrentStoreAsync()).Id);

                if (cart.Any())
                {
                    var termLookup = _termLookupService.GetTerm(cart);
                    HttpContext.Session.Set("TransPromo", termLookup.termNo ?? DefaultTransPromo);
                    HttpContext.Session.SetString("TransDescription", $"{termLookup.description} {termLookup.link}");
                }
            }
            catch (IsamException e)
            {
                _logger.Error("Failure occurred during ISAM Term Lookup", e);
                HttpContext.Session.SetString("TransPromo", DefaultTransPromo);
            }
            return "";
        }

        private void SendPaymentRequest(ProcessPaymentRequest paymentInfo, out string status_code, out string response_message)
        {
            status_code = "";
            response_message = "";

            var cart = await _shoppingCartService.GetShoppingCartAsync(
                    await _workContext.GetCurrentCustomerAsync(),
                    ShoppingCartType.ShoppingCart,
                    (await _storeContext.GetCurrentStoreAsync()).Id);
            var customer = _customerService.GetCustomerById(
                cart.FirstOrDefault().CustomerId
            );
            var billingAddress = _customerService.GetCustomerBillingAddress(
                customer
            );
            var domain = await _storeContext.GetCurrentStoreAsync().Url;
            var ip = _webHelper.GetCurrentIpAddress();

            try
            {
                var cardCheck = _cardCheckService.CheckCard(
                    paymentInfo,
                    billingAddress,
                    domain,
                    ip
                );

                HttpContext.Session.Set("Auth_No", cardCheck.AuthNo ?? "");
                HttpContext.Session.SetString("Ref_No", cardCheck.RefNo ?? "");
                status_code = cardCheck.StatusCode ?? "00";
                response_message = cardCheck.ResponseMessage;
            }
            catch (Exception e)
            {
                _logger.Error("Error occurred when making external payment request. Setting status code to 00 and Ref_No Auth_No to null.", e);
                status_code = "00";
                HttpContext.Session.SetString("Ref_No", "");
                HttpContext.Session.Set("Auth_No", "");
            }
        }

        private void ValidateGiftCardAmounts()
        {
            // grab every gift card that the customer applied to this order
            IList<GiftCard> appliedGiftCards = _giftCardService.GetActiveGiftCardsAppliedByCustomer(await _workContext.GetCurrentCustomerAsync());

            if (appliedGiftCards.Count > 1)
            {
                throw new Exception("Only one gift card may be applied to an order");
            }

            foreach (GiftCard nopGiftCard in appliedGiftCards)
            {
                // check isam to make sure each gift card has the right $$
                GiftCard isamGiftCard = _isamGiftCardService.GetGiftCardInfo(nopGiftCard.GiftCardCouponCode).GiftCard;

                decimal nopAmtLeft = nopGiftCard.Amount;
                List<GiftCardUsageHistory> nopGcHistory = _giftCardService.GetGiftCardUsageHistory(nopGiftCard).ToList();

                foreach (var history in nopGcHistory)
                {
                    nopAmtLeft -= history.UsedValue;
                }

                if (isamGiftCard.Amount != nopAmtLeft)
                {
                    throw new Exception("A gift card has been used since it was placed on this order");

                }
            }
        }

        #endregion

        #region Utilities
        [NonAction]
        private IDictionary<ShoppingCartItem, List<ProductAttributeValue>> GetWarranties(
            IList<ShoppingCartItem> cart
        )
        {
            var warranties =
                new Dictionary<ShoppingCartItem, List<ProductAttributeValue>>();
            foreach (var sci in cart)
            {

                var mappings =
                    _productAttributeService.GetProductAttributeMappingsByProductId(
                        sci.ProductId
                    ).Where(
                        pam =>
                        _productAttributeService.GetProductAttributeById(pam.ProductAttributeId) != null &&
                        _productAttributeService.GetProductAttributeById(pam.ProductAttributeId).Name == "Warranty");

                var options = new List<ProductAttributeValue>();
                foreach (var mapping in mappings)
                {
                    options.AddRange(
                        _productAttributeService.GetProductAttributeValues(mapping.Id));
                }
                if (options.Any())
                {
                    warranties.Add(sci, options);
                }
            }

            return warranties;
        }

        #endregion

        // Makes an external request
        [HttpPost, ActionName("ShippingMethod")]
        [FormValueRequired("nextstep")]
        public IActionResult SelectShippingMethod(string shippingoption, IFormCollection form)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(await _workContext.GetCurrentCustomerAsync()) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            if (!_shoppingCartService.ShoppingCartRequiresShipping(cart))
            {
                _genericAttributeService.SaveAttribute<ShippingOption>(await _workContext.GetCurrentCustomerAsync(),
                    NopCustomerDefaults.SelectedShippingOptionAttribute, null, (await _storeContext.GetCurrentStoreAsync()).Id);
                return RedirectToRoute("CheckoutPaymentMethod");
            }

            //pickup point
            if (_shippingSettings.AllowPickupInStore && _orderSettings.DisplayPickupInStoreOnShippingMethodPage)
            {
                var pickupInStore = ParsePickupInStore(form);
                if (pickupInStore)
                {
                    var pickupOption = ParsePickupOption(form);
                    SavePickupOption(pickupOption);

                    return RedirectToRoute("CheckoutPaymentMethod");
                }

                //set value indicating that "pick up in store" option has not been chosen
                _genericAttributeService.SaveAttribute<PickupPoint>(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.SelectedPickupPointAttribute, null, (await _storeContext.GetCurrentStoreAsync()).Id);
            }

            //parse selected method 
            if (string.IsNullOrEmpty(shippingoption))
                return RedirectToAction("ShippingMethod");
            var splittedOption = shippingoption.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries);
            if (splittedOption.Length != 2)
                return RedirectToAction("ShippingMethod");
            var selectedName = splittedOption[0];
            var shippingRateComputationMethodSystemName = splittedOption[1];

            //find it
            //performance optimization. try cache first
            var shippingOptions = await _genericAttributeService.GetAttributeAsync<List<ShippingOption>>(await _workContext.GetCurrentCustomerAsync(),
                NopCustomerDefaults.OfferedShippingOptionsAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);
            if (shippingOptions == null || !shippingOptions.Any())
            {
                //not found? let's load them using shipping service
                shippingOptions = _shippingService.GetShippingOptions(cart, _customerService.GetCustomerShippingAddress(await _workContext.GetCurrentCustomerAsync()),
                    await _workContext.GetCurrentCustomerAsync(), shippingRateComputationMethodSystemName, (await _storeContext.GetCurrentStoreAsync()).Id).ShippingOptions.ToList();
            }
            else
            {
                //loaded cached results. let's filter result by a chosen shipping rate computation method
                shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(shippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
            }

            var shippingOption = shippingOptions
                .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedName, StringComparison.InvariantCultureIgnoreCase));
            if (shippingOption == null)
                return RedirectToAction("ShippingMethod");

            //save
            _genericAttributeService.SaveAttribute(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, (await _storeContext.GetCurrentStoreAsync()).Id);

            SendExternalShippingMethodRequest();

            if (_warrantyService.CartContainsWarranties(cart))
            {
                return RedirectToRoute("WarrantySelection");
            }

            return RedirectToRoute("CheckoutPaymentMethod");
        }

        public IActionResult WarrantySelection()
        {
            var cart = await _shoppingCartService.GetShoppingCartAsync(
                await _workContext.GetCurrentCustomerAsync(),
                ShoppingCartType.ShoppingCart,
                (await _storeContext.GetCurrentStoreAsync()).Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(await _workContext.GetCurrentCustomerAsync()) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            return View(GetWarranties(cart));
        }

        [HttpPost, ActionName("WarrantySelection")]
        [FormValueRequired("nextstep")]
        public IActionResult SelectWarranty(IFormCollection form)
        {
            var cart = await _shoppingCartService.GetShoppingCartAsync(
                await _workContext.GetCurrentCustomerAsync(),
                ShoppingCartType.ShoppingCart,
                (await _storeContext.GetCurrentStoreAsync()).Id);

            SaveWarranty(cart, form);

            return RedirectToRoute("CheckoutPaymentMethod");
        }

        private void SaveWarranty(IList<ShoppingCartItem> cart, IFormCollection form)
        {
            foreach (var keyValue in GetWarranties(cart))
            {
                var value = form[keyValue.Key.Id.ToString()];
                var sci = keyValue.Key;

                //Remove currently selected warranty
                var warranties = _productAttributeParser.ParseProductAttributeValues(
                    sci.AttributesXml)
                    .Where(val => _productAttributeService.GetProductAttributeById(
                            _productAttributeService.GetProductAttributeMappingById(val.ProductAttributeMappingId).ProductAttributeId
                            ).Name == "Warranty")
                    .ToList();

                if (warranties.Count > 0)
                {
                    var pam =
                        _productAttributeService.GetProductAttributeMappingsByProductId(
                            await _productService.GetProductByIdAsync(sci.ProductId).Id)
                        .Where(map => _productAttributeService.GetProductAttributeById(map.ProductAttributeId).Name == "Warranty").Single();

                    sci.AttributesXml =
                        _productAttributeParser.RemoveProductAttribute(sci.AttributesXml, pam);
                }

                if (value != "NoWarranty")
                {
                    var pav = _productAttributeService.GetProductAttributeValueById(
                        int.Parse(value));

                    sci.AttributesXml = _productAttributeParser.AddProductAttribute(
                        sci.AttributesXml, _productAttributeService.GetProductAttributeMappingById(pav.ProductAttributeMappingId), pav.Id.ToString());
                }

                // get checkout state before it gets deleted

                // hold OfferedShippingOptions
                var shippingOptions =
                    await _genericAttributeService.GetAttributeAsync<List<ShippingOption>>(
                        await _workContext.GetCurrentCustomerAsync(),
                        NopCustomerDefaults.OfferedShippingOptionsAttribute,
                        (await _storeContext.GetCurrentStoreAsync()).Id);

                // hold selected shipping option
                var selectedShippingOption =
                    await _genericAttributeService.GetAttributeAsync<ShippingOption>(
                        await _workContext.GetCurrentCustomerAsync(),
                        NopCustomerDefaults.SelectedShippingOptionAttribute,
                        (await _storeContext.GetCurrentStoreAsync()).Id);

                // hold SelectedPaymentMethod
                //find a selected (previously) payment method
                var selectedPaymentMethodSystemName =
                    await _genericAttributeService.GetAttributeAsync<string>(
                        await _workContext.GetCurrentCustomerAsync(),
                        NopCustomerDefaults.SelectedPaymentMethodAttribute,
                        (await _storeContext.GetCurrentStoreAsync()).Id);

                // updating shopping cart resets the order state, so we are going to save the state, and re-initialize it after the reset
                await _shoppingCartService.UpdateShoppingCartItemAsync(
                    _customerService.GetCustomerById(sci.CustomerId),
                    sci.Id,
                    sci.AttributesXml,
                    sci.CustomerEnteredPrice,
                    sci.RentalStartDateUtc,
                    sci.RentalEndDateUtc,
                    sci.Quantity);

                // re-add shopping cart state to what it was before

                // OfferedShippingOptions
                _genericAttributeService.SaveAttribute(await _workContext.GetCurrentCustomerAsync(),
                                                       NopCustomerDefaults.OfferedShippingOptionsAttribute,
                                                       shippingOptions,
                                                       (await _storeContext.GetCurrentStoreAsync()).Id);

                // SelectedShippingOption
                _genericAttributeService.SaveAttribute(await _workContext.GetCurrentCustomerAsync(),
                    NopCustomerDefaults.SelectedShippingOptionAttribute,
                    selectedShippingOption,
                    (await _storeContext.GetCurrentStoreAsync()).Id);

                // SelectedPaymentMethod
                _genericAttributeService.SaveAttribute(await _workContext.GetCurrentCustomerAsync(),
                                                       NopCustomerDefaults.SelectedPaymentMethodAttribute,
                                                       selectedPaymentMethodSystemName,
                                                       (await _storeContext.GetCurrentStoreAsync()).Id);
            }
        }

        // Custom - uses CC_REF_NO and AUTH_CODE
        [HttpPost, ActionName("Confirm")]
        public IActionResult ConfirmOrder()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = await _shoppingCartService.GetShoppingCartAsync(
                await _workContext.GetCurrentCustomerAsync(),
                ShoppingCartType.ShoppingCart,
                (await _storeContext.GetCurrentStoreAsync()).Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(await _workContext.GetCurrentCustomerAsync()) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            ValidateGiftCardAmounts();

            //model
            var model = _checkoutModelFactory.PrepareConfirmOrderModel(cart);
            try
            {
                //prevent 2 orders being placed within an X seconds time frame
                if (!IsMinimumOrderPlacementIntervalValid(await _workContext.GetCurrentCustomerAsync()))
                    throw new Exception(await _localizationService.GetResourceAsync( "Checkout.MinOrderPlacementInterval"));

                //place order
                var processPaymentRequest = HttpContext.Session.Get<ProcessPaymentRequest>("OrderPaymentInfo");
                if (processPaymentRequest == null)
                {
                    //Check whether payment workflow is required
                    if (await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart))
                        return RedirectToRoute("CheckoutPaymentInfo");

                    processPaymentRequest = new ProcessPaymentRequest();
                }

                _paymentService.GenerateOrderGuid(processPaymentRequest);
                processPaymentRequest.StoreId = (await _storeContext.GetCurrentStoreAsync()).Id;
                processPaymentRequest.CustomerId = await _workContext.GetCurrentCustomerAsync().Id;
                processPaymentRequest.PaymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(await _workContext.GetCurrentCustomerAsync(),
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);
                HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", processPaymentRequest);

                // Set ABC custom values
                var refNo = HttpContext.Session.GetString("Ref_No");
                if (refNo != null)
                {
                    processPaymentRequest.CustomValues.Add("CC_REFNO", refNo);
                    HttpContext.Session.Remove("Ref_No");
                }
                var authNo = HttpContext.Session.GetString("Auth_No");
                if (authNo != null)
                {
                    processPaymentRequest.CustomValues.Add("AuthCode", authNo);
                    HttpContext.Session.Remove("Auth_No");
                }

                var placeOrderResult = _orderProcessingService.PlaceOrder(processPaymentRequest);

                if (placeOrderResult.Success)
                {
                    HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", null);
                    var postProcessPaymentRequest = new PostProcessPaymentRequest
                    {
                        Order = placeOrderResult.PlacedOrder
                    };
                    _paymentService.PostProcessPayment(postProcessPaymentRequest);

                    if (_webHelper.IsRequestBeingRedirected || _webHelper.IsPostBeingDone)
                    {
                        //redirection or POST has been done in PostProcessPayment
                        return Content(await _localizationService.GetResourceAsync( "Checkout.RedirectMessage"));
                    }

                    return RedirectToRoute("CheckoutCompleted", new { orderId = placeOrderResult.PlacedOrder.Id });
                }

                foreach (var error in placeOrderResult.Errors)
                    model.Warnings.Add(error);
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc);
                model.Warnings.Add(exc.Message);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        // Custom, allows for AJAX calls
        [HttpPost, ActionName("PaymentInfo")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> EnterPaymentInfo(IFormCollection form)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = await _shoppingCartService.GetShoppingCartAsync(
                await _workContext.GetCurrentCustomerAsync(),
                ShoppingCartType.ShoppingCart,
                (await _storeContext.GetCurrentStoreAsync()).Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (await _customerService.IsGuestAsync(await _workContext.GetCurrentCustomerAsync()) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //Check whether payment workflow is required
            var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart);
            if (!isPaymentWorkflowRequired)
            {
                return RedirectToRoute("CheckoutConfirm");
            }

            //load payment method
            var paymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(await _workContext.GetCurrentCustomerAsync(),
                NopCustomerDefaults.SelectedPaymentMethodAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);
            var paymentMethod = await _paymentPluginManager
                .LoadPluginBySystemNameAsync(paymentMethodSystemName, await _workContext.GetCurrentCustomerAsync(), (await _storeContext.GetCurrentStoreAsync()).Id);
            if (paymentMethod == null)
                return RedirectToRoute("CheckoutPaymentMethod");

            var warnings = await paymentMethod.ValidatePaymentFormAsync(form);
            foreach (var warning in warnings)
                ModelState.AddModelError("", warning);

            // custom code is here
            string status_code = string.Empty;
            string response_message = string.Empty;
            var isAjaxCall = Convert.ToBoolean(form["is_ajax_call"]);

            if (ModelState.IsValid)
            {
                //get payment info
                var paymentInfo = await paymentMethod.GetPaymentInfoAsync(form);
                //set previous order GUID (if exists)
                _paymentService.GenerateOrderGuid(paymentInfo);

                HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", paymentInfo);

                // If calling this route via AJAX (for Synchrony plugin), 
                // return 200 so the plugin can handle redirecting to /confirm
                if (isAjaxCall)
                {
                    return new OkResult();
                }
                SendPaymentRequest(paymentInfo, out status_code, out response_message);

                if (status_code == "00")
                {
                    //session save
                    HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", paymentInfo);
                    return RedirectToRoute("CheckoutConfirm");
                }
                else
                {
                    response_message = WebUtility.HtmlDecode(response_message);
                    ModelState.AddModelError("", response_message);
                }
            }

            //If we got this far, something failed, redisplay form
            //model
            var model = await _checkoutModelFactory.PreparePaymentInfoModelAsync(paymentMethod);
            return View(model);
        }
    }
}
