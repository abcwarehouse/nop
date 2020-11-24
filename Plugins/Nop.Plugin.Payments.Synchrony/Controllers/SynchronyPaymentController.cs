using System;
using System.Collections.Generic;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;
using Nop.Core;
using Nop.Services.Stores;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Synchrony.Models;
using System.Net;
using System.IO;
using System.Linq;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Tax;
using Nop.Core.Infrastructure;
using Nop.Services.Common;
using Nop.Core.Caching;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Messages;
using Nop.Core.Http.Extensions;
using Nop.Web.Models.Checkout;
using Nop.Web.Factories;
using Nop.Web.Models.ShoppingCart;
using System.Text.Json;
using Nop.Services.Customers;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.AbcCore.Services;

namespace Nop.Plugin.Payments.Synchrony.Controllers
{
    public class SynchronyPaymentController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;
        private readonly SynchronyPaymentSettings _synchronyPaymentSettings;
        private readonly OrderSettings _orderSettings;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ITaxService _taxService;
        private readonly IPriceCalculationService _priceCalculationService;

        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IHttpContextAccessor _httpContext = EngineContext.Current.Resolve<IHttpContextAccessor>();
        private readonly IGenericAttributeService _genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
        private readonly INotificationService _notificationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductService _productService;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly IAddressService _addressService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICustomerService _customerService;
        private readonly ICheckoutModelFactory _checkoutModelFactory;
        private readonly IPaymentPluginManager _paymentPluginManager;

        private readonly IGiftCardService _giftCardService;
        private readonly IIsamGiftCardService _isamGiftCardService;

        public SynchronyPaymentController(
            IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            IPaymentService paymentService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            ILogger logger,
            IWebHelper webHelper,
            PaymentSettings paymentSettings,
            SynchronyPaymentSettings synchronyPaymentSettings,
            OrderSettings orderSettings,
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            ITaxService taxService,
            IPriceCalculationService priceCalculationService,
            IProductAttributeFormatter productAttributeFormatter,
            IProductAttributeParser productAttributeParser,
            IStaticCacheManager staticCacheManager,
            INotificationService notificationService,
            IShoppingCartService shoppingCartService,
            IProductService productService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            IAddressService addressService,
            IStateProvinceService stateProvinceService,
            ICustomerService customerService,
            ICheckoutModelFactory checkoutModelFactory,
            IPaymentPluginManager paymentPluginManager,
            IGiftCardService giftCardService,
            IIsamGiftCardService isamGiftCardService
        )
        {
            _workContext = workContext;
            _storeService = storeService;
            _settingService = settingService;
            _paymentService = paymentService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _localizationService = localizationService;
            _storeContext = storeContext;
            _logger = logger;
            _webHelper = webHelper;
            _paymentSettings = paymentSettings;
            _synchronyPaymentSettings = synchronyPaymentSettings;
            _orderSettings = orderSettings;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _taxService = taxService;
            _priceCalculationService = priceCalculationService;
            _productAttributeFormatter = productAttributeFormatter;
            _productAttributeParser = productAttributeParser;
            _staticCacheManager = staticCacheManager;
            _notificationService = notificationService;
            _shoppingCartService = shoppingCartService;
            _productService = productService;
            _shoppingCartModelFactory = shoppingCartModelFactory;
            _addressService = addressService;
            _stateProvinceService = stateProvinceService;
            _customerService = customerService;
            _checkoutModelFactory = checkoutModelFactory;
            _paymentPluginManager = paymentPluginManager;
            _giftCardService = giftCardService;
            _isamGiftCardService = isamGiftCardService;
        }

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var paySynchronyPaymentSettings = _settingService.LoadSetting<SynchronyPaymentSettings>(storeScope);
            
            return View(
                "~/Plugins/Payments.Synchrony/Views/Configure.cshtml",
                paySynchronyPaymentSettings.ToModel(storeScope)
            );
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [AutoValidateAntiforgeryToken]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var paySynchronyPaymentSettings = SynchronyPaymentSettings.FromModel(model);

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.MerchantId, model.MerchantId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.MerchantPassword, model.MerchantPassword_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.Integration, model.Integration_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.TokenNumber, model.TokenNumber_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.WhitelistDomain, model.WhitelistDomain_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.DemoEndPoint, model.DemoEndPoint_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.LiveEndPoint, model.LiveEndPoint_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.ServerURL, model.ServerURL_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.authorizationEndPoint_Demo, model.authorizationEndPoint_Demo_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.authorizationEndPoint_Live, model.authorizationEndPoint_Live_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(paySynchronyPaymentSettings, x => x.IsDebugMode, model.IsDebugMode_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [HttpPost]
        public IActionResult PaymentPostInfo()
        {
            decimal transactionAmount = 0;
            var cart = _shoppingCartService.GetShoppingCart(
                _workContext.CurrentCustomer,
                ShoppingCartType.ShoppingCart,
                _storeContext.CurrentStore.Id);

            // why do we need to create a ShoppingCartModel?
            var shoppingCartModel = _shoppingCartModelFactory.PrepareShoppingCartModel(new ShoppingCartModel(), cart);

            foreach (var sci in shoppingCartModel.Items)
            {
                transactionAmount += Convert.ToDecimal(sci.SubTotal.Replace("$", ""));
            }

            AuthenticationTokenResponse model = new AuthenticationTokenResponse();

            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var paySynchronyPaymentSettings = _settingService.LoadSetting<SynchronyPaymentSettings>(storeScope);

            //Stored Merchant Id & Password & TokenId
            var merchantId = paySynchronyPaymentSettings.MerchantId;
            var merchantPassword = paySynchronyPaymentSettings.MerchantPassword;
            var Integration = paySynchronyPaymentSettings.Integration;
            var token = HttpContext.Session.GetString("token").Replace("\"", "");

            //Find Status Endpoint
            string authorizationRegionStatusURL = paySynchronyPaymentSettings.Integration == true
                ? paySynchronyPaymentSettings.DemoEndPoint
                : paySynchronyPaymentSettings.LiveEndPoint;

            try
            {

                #region Find Status Call
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(authorizationRegionStatusURL);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonSerializer.Serialize(new
                    {
                        merchantNumber = merchantId,
                        password = merchantPassword,
                        userToken = token
                    });
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                var customer = _workContext.CurrentCustomer;

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();

                    if (_synchronyPaymentSettings.IsDebugMode)
                    {
                        _logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, $"PaymentPostInfo response - {result}");
                    }

                    var authResponse = JsonSerializer.Deserialize<AuthenticationTokenResponse>(result);
                    var billingAddress = _addressService.GetAddressById(customer.BillingAddressId.Value);
                    model.Integration = Integration;
                    model.MerchantId = merchantId;
                    model.clientToken = token;
                    model.transactionId = authResponse.transactionId;
                    model.responseCode = authResponse.responseCode;
                    model.responseDesc = authResponse.responseDesc;
                    model.ZipCode = billingAddress.ZipPostalCode;
                    model.State = _stateProvinceService.GetStateProvinceByAddress(
                        billingAddress
                    ).Abbreviation;
                    model.FirstName = billingAddress.FirstName;
                    model.City = billingAddress.City;
                    model.Address1 = billingAddress.Address1;
                    model.LastName = billingAddress.LastName;
                    model.StatusCode = authResponse.StatusCode;
                    model.AccountNumber = authResponse.AccountNumber;
                    model.StatusMessage = authResponse.StatusMessage;
                    model.TransactionAmount = transactionAmount.ToString();

                    //session save
                    HttpContext.Session.Set("syfPaymentInfo", model);

                    if (model.StatusMessage != "Account Authentication Success")
                    {
                        var errorModel = new ErrorModel()
                        {
                            Message = model.StatusMessage,
                            isBack = true
                        };
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel
                            {
                                name = "Error",
                                html = RenderPartialViewToString("~/Plugins/Payments.Synchrony/Views/ErrorMessagepopup.cshtml", errorModel)
                            },
                            goto_section = "Error"
                        });
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.Warning(ex.Message, ex, _workContext.CurrentCustomer);
                var errorModel = new ErrorModel()
                {
                    Message = ex.Message,
                    isBack = true
                };
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "Error",
                        html = RenderPartialViewToString("~/Plugins/Payments.Synchrony/Views/ErrorMessagepopup.cshtml", errorModel)
                    },
                    goto_section = "Error"
                });
            }

            TempData["SecondModalpopup"] = JsonSerializer.Serialize(model);
            return Json(new
            {
                update_section = new UpdateSectionJsonModel
                {
                    name = "Third-Modal-Method",
                    html = RenderPartialViewToString("~/Plugins/Payments.Synchrony/Views/ThirdModalpopup.cshtml", model)
                },
                goto_section = "Third_Modal_Method"
            });
        }
		
        // uses custom view
		public IActionResult Confirm()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = _shoppingCartService.GetShoppingCart(
                _workContext.CurrentCustomer,
                ShoppingCartType.ShoppingCart,
                _storeContext.CurrentStore.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (_customerService.IsGuest(_workContext.CurrentCustomer) &&
                !_orderSettings.AnonymousCheckoutAllowed)
            {
                return Challenge();
            }
                
            //model
            var model = PrepareConfirmOrderModel(cart);
            
            return View("~/Plugins/Payments.Synchrony/Views/Confirm.cshtml", model);
        }

        [HttpPost]
        public JsonResult PaymentPostInfoStatus()
        {
            AuthenticationTokenResponse model = new AuthenticationTokenResponse();

            var cart = _shoppingCartService.GetShoppingCart(
                _workContext.CurrentCustomer,
                ShoppingCartType.ShoppingCart,
                _storeContext.CurrentStore.Id);

            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var paySynchronyPaymentSettings = _settingService.LoadSetting<SynchronyPaymentSettings>(storeScope);

            //Stored Merchant Id & Password & TokenId
            var merchantId = paySynchronyPaymentSettings.MerchantId;
            var merchantPassword = paySynchronyPaymentSettings.MerchantPassword;
            var Integration = paySynchronyPaymentSettings.Integration;
            var token = HttpContext.Session.GetString("token").Replace("\"", "");

            //Find Status Endpoint
            string authorizationRegionStatusURL = paySynchronyPaymentSettings.Integration == true
                ? paySynchronyPaymentSettings.DemoEndPoint
                : paySynchronyPaymentSettings.LiveEndPoint;

            try
            {

                #region Find Status Call
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(authorizationRegionStatusURL);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonSerializer.Serialize(new
                    {
                        merchantNumber = merchantId,
                        password = merchantPassword,
                        userToken = token
                    });
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var customer = _workContext.CurrentCustomer;

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();

                    var authResponse = JsonSerializer.Deserialize<AuthenticationTokenResponse>(result);
                    var billingAddress = _addressService.GetAddressById(customer.BillingAddressId.Value);

                    model.MerchantId = merchantId;
                    model.clientToken = authResponse.TokenId;
                    model.ClientTransactionID = authResponse.ClientTransactionID;
                    model.FirstName = billingAddress.FirstName;
                    model.LastName = billingAddress.LastName;
                    model.responseCode = authResponse.responseCode;
                    model.responseDesc = authResponse.responseDesc;
                    model.StatusCode = authResponse.StatusCode;
                    model.StatusMessage = authResponse.StatusMessage;
                    model.AccountNumber = authResponse.AccountNumber;
                    model.transactionId = authResponse.transactionId;
                    model.TransactionAmount = authResponse.TransactionAmount;
                    model.TransactionDate = authResponse.TransactionDate;
                    model.TransactionDescription = authResponse.TransactionDescription;
                    model.AuthCode = authResponse.AuthCode;
                    model.PromoCode = authResponse.PromoCode;
                    model.PostbackId = authResponse.PostbackId;

                    //session save
                    HttpContext.Session.Set("syfPaymentInfo", model);

                    if (model.StatusMessage != "Auth Approved")
                    {
                        var errorModel = new ErrorModel()
                        {
                            Message = model.StatusMessage,
                            isBack = false
                        };
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel
                            {
                                name = "Error",
                                html = RenderPartialViewToString("~/Plugins/Payments.Synchrony/Views/ErrorMessagepopup.cshtml", errorModel)
                            },
                            goto_section = "Error"
                        });
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.Warning(ex.Message, ex, _workContext.CurrentCustomer);
                var errorModel = new ErrorModel()
                {
                    Message = ex.Message,
                    isBack = false
                };
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "Error",
                        html = RenderPartialViewToString("~/Plugins/Payments.Synchrony/Views/ErrorMessagepopup.cshtml", errorModel)
                    },
                    goto_section = "Error"
                });
            }
            return Json(new { success = 1 });
        }

        [HttpPost]
        public IActionResult SavePaymentInfo(IFormCollection form)
        {
            try
            {
                //validation
                var cart = _shoppingCartService.GetShoppingCart(
                    _workContext.CurrentCustomer,
                    ShoppingCartType.ShoppingCart,
                    _storeContext.CurrentStore.Id);

                if (_customerService.IsGuest(_workContext.CurrentCustomer) &&
                    !_orderSettings.AnonymousCheckoutAllowed)
                {
                    throw new Exception("Anonymous checkout is not allowed");
                }

                var paymentMethodSystemName = _genericAttributeService.GetAttribute<string>(
                    _workContext.CurrentCustomer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute,
                    _storeContext.CurrentStore.Id);
                var paymentMethod = _paymentPluginManager.LoadPluginBySystemName(
                    paymentMethodSystemName,
                    _workContext.CurrentCustomer,
                    _storeContext.CurrentStore.Id);
                if (paymentMethod == null)
                    throw new Exception("Payment method is not selected");

                var warnings = paymentMethod.ValidatePaymentForm(form);
                foreach (var warning in warnings)
                    ModelState.AddModelError("", warning);

                if (ModelState.IsValid)
                {
                    //get payment info
                    var paymentInfo = paymentMethod.GetPaymentInfo(form);
                    //set previous order GUID (if exists)
                    _paymentService.GenerateOrderGuid(paymentInfo);

                    //session save
                    HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);
                    if (paymentInfo != null)
                    {
                        return Json(new { status = true });
                    }
					else
                    {
                        var errorModel = new ErrorModel()
                        {
                            Message = _localizationService.GetResource("Plugins.Payments.Synchrony.paymentInfonotFound"),
                            isBack = true
                        };
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel
                            {
                                name = "Error",
                                html = this.RenderPartialViewToString(
                                    "~/Plugins/Payments.Synchrony/Views/ErrorMessagepopup.cshtml",
                                    errorModel
                                )
                            },
                            goto_section = "Error",
                            status = false
                        });
                    }
                }

                //If we got this far, something failed, redisplay form
                var paymenInfoModel = _checkoutModelFactory.PreparePaymentInfoModel(paymentMethod);
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "payment-info",
                        html = this.RenderPartialViewToString("OpcPaymentInfo", paymenInfoModel)
                    }
                });
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [NonAction]
        protected virtual CustomCheckoutConfirmModel PrepareConfirmOrderModel(IList<ShoppingCartItem> cart)
        {
            var model = new CustomCheckoutConfirmModel
            {
                TermsOfServiceOnOrderConfirmPage = _orderSettings.TermsOfServiceOnOrderConfirmPage
            };
            //min order amount validation
            bool minOrderTotalAmountOk = _orderProcessingService.ValidateMinOrderTotalAmount(cart);
            if (!minOrderTotalAmountOk)
            {
                decimal minOrderTotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount, _workContext.WorkingCurrency);
                model.MinOrderTotalWarning = string.Format(_localizationService.GetResource("Checkout.MinOrderTotalAmount"), _priceFormatter.FormatPrice(minOrderTotalAmount, true, false));
            }

            model.SynchronyAuthTokenResponse = GetSynchronyAuthTokenResponse();

            return model;
        }

        private AuthenticationTokenResponse GetSynchronyAuthTokenResponse()
        {
            var authResponseJson = TempData["SecondModalpopup"].ToString();
            TempData.Keep("SecondModalpopup");
            var authTokenResponse = 
                JsonSerializer.Deserialize<AuthenticationTokenResponse>(authResponseJson);

            if (authTokenResponse == null || authTokenResponse.StatusMessage != "Account Authentication Success")
            {
                throw new Exception("Unable to retrieve AuthenticationTokenResponse");
            }

            var transPromo = HttpContext.Session.GetString("TransPromo").Replace("\"", "");
            var sessionOrderTotal = HttpContext.Session.GetString("OrderTotal");
            if (!string.IsNullOrEmpty(transPromo))
            {
                authTokenResponse.PromoCode = transPromo;
            }
            if (!string.IsNullOrEmpty(sessionOrderTotal))
            {
                authTokenResponse.TransactionAmount = sessionOrderTotal;
            }

            return authTokenResponse;
        }

        [HttpGet]
        public void parseDbuyJsonCallBack(string callbackMessage)
        {
            if (callbackMessage != null)
            {
                var authResponse = JsonSerializer.Deserialize<AuthenticationTokenResponse>(callbackMessage);
            }
        }
        #endregion

        private bool isValidAuthTokenResponse(string message)
        {
            return message == "Auth Approved" || message == "Account Authentication Success";
        }

        private void ValidateGiftCardAmounts()
        {
            // grab every gift card that the customer applied to this order
            IList<GiftCard> appliedGiftCards = _giftCardService.GetActiveGiftCardsAppliedByCustomer(_workContext.CurrentCustomer);

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

        protected virtual bool IsMinimumOrderPlacementIntervalValid(Customer customer)
        {
            //prevent 2 orders being placed within an X seconds time frame
            if (_orderSettings.MinimumOrderPlacementInterval == 0)
                return true;

            var lastOrder = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                .FirstOrDefault();
            if (lastOrder == null)
                return true;

            var interval = DateTime.UtcNow - lastOrder.CreatedOnUtc;
            return interval.TotalSeconds > _orderSettings.MinimumOrderPlacementInterval;
        }
    }
}
