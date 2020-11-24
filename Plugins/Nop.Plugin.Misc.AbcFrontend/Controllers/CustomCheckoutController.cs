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
        private readonly IOrderService _orderService;
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
            IOrderService orderService,
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
            IWarrantyService warrantyService
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

            DefaultTransPromo = _settingService.GetSetting("ordersettings.defaulttranspromo")?.Value;

            if (string.IsNullOrWhiteSpace(DefaultTransPromo))
            {
                throw new ConfigurationErrorsException("'ordersettings.defaulttranspromo' setting is required for CustomCheckoutController.");
            }
        }

        #region Methods (one page checkout)
        private string SendExternalShippingMethodRequest()
        {
            if (DefaultTransPromo == "101" || _coreSettings.AreExternalCallsSkipped)
            {
                _logger.Warning("External shipping method request skipped.");
                HttpContext.Session.Set("TransPromo", DefaultTransPromo);
                return string.Empty;
            }

            try
            {
                var cart = _shoppingCartService.GetShoppingCart(
                    _workContext.CurrentCustomer,
                    ShoppingCartType.ShoppingCart,
                    _storeContext.CurrentStore.Id);

                if (cart.Any())
                {
                    string xml = "";
                    xml = $"<Request>{Environment.NewLine}<Term_Lookup>{Environment.NewLine}<Items>";
                    foreach (var item in cart)
                    {
                        var product = _productService.GetProductById(item.ProductId);
                        xml += $"{Environment.NewLine}<Item>{Environment.NewLine}<Sku>{product.Sku}</Sku>{Environment.NewLine}<Gtin>{product.Gtin}</Gtin>{Environment.NewLine}<Qty>{item.Quantity}</Qty>{Environment.NewLine}<Brand>{product.Name}</Brand>{Environment.NewLine}<Price>{product.Price}</Price>{Environment.NewLine}</Item>";
                    }
                    xml += $"{Environment.NewLine}</Items>{Environment.NewLine}</Term_Lookup>{Environment.NewLine}</Request>";

                    var webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.CreateHttp(AbcConstants.StatusAPIUrl);
                    webRequest.Method = "POST";
                    webRequest.ContentType = "text/xml; charset=utf-8";

                    byte[] byteArray = Encoding.UTF8.GetBytes(xml);
                    webRequest.ContentLength = byteArray.Length;
                    using (System.IO.Stream requestStream = webRequest.GetRequestStream())
                    {
                        requestStream.Write(byteArray, 0, byteArray.Length);
                    }

                    System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)webRequest.GetResponse();
                    System.IO.Stream r_stream = response.GetResponseStream();

                    using (System.IO.StreamReader reader = new System.IO.StreamReader(r_stream))
                    {
                        string strResponse = reader.ReadToEnd();

                        if (!string.IsNullOrEmpty(strResponse))
                        {
                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(strResponse);

                            string xpath = "Response/Term_Lookup/Term_No";
                            var term_no = xmlDoc.SelectSingleNode(xpath);
                            if (term_no != null && !string.IsNullOrEmpty(term_no.InnerText))
                            {
                                HttpContext.Session.Set("TransPromo", term_no.InnerText);
                            }
                            else
                            {
                                HttpContext.Session.Set("TransPromo", DefaultTransPromo);
                            }

                            xpath = "Response/Term_Lookup/Description";
                            var description = xmlDoc.SelectSingleNode(xpath);
                            if (description != null)
                            {
                                xpath = "Response/Term_Lookup/Link";
                                var link = xmlDoc.SelectSingleNode(xpath);
                                string linkText = string.Empty;
                                if (link != null)
                                {
                                    linkText = link.InnerText;
                                }

                                HttpContext.Session.SetString("TransDescription", description.InnerText + " " + linkText);
                            }
                            
                        }
                    }
                }
            }
            catch
            {
                HttpContext.Session.SetString("TransPromo", DefaultTransPromo);
            }
            return "";
        }

		private void SendPaymentRequest(ProcessPaymentRequest paymentInfo, out string status_code, out string response_message)
		{
            status_code = "";
            response_message = "";

            if (_coreSettings.AreExternalCallsSkipped)
            {
                status_code = "00";
                return;
            }

			try
			{				
				var cart = _shoppingCartService.GetShoppingCart(
                    _workContext.CurrentCustomer,
                    storeId: _storeContext.CurrentStore.Id);
                
				string domainName = _storeContext.CurrentStore.Url;

				if (cart.Any())
				{
                    var customer = _customerService.GetCustomerById(cart.FirstOrDefault().CustomerId);
                    var billingAddress = _customerService.GetCustomerBillingAddress(customer);
					string xml = "";					
					xml = $"<Request>{Environment.NewLine}";
					xml += $"<Card_Check>{Environment.NewLine}";
					xml += $"<Card_Number>{paymentInfo.CreditCardNumber}</Card_Number>{Environment.NewLine}";
					xml += $"<Exp_Month>{paymentInfo.CreditCardExpireMonth}</Exp_Month>{Environment.NewLine}";
					xml += $"<Exp_Year>{paymentInfo.CreditCardExpireYear}</Exp_Year>{Environment.NewLine}";
					xml += $"<Cvv2>{paymentInfo.CreditCardCvv2}</Cvv2>{Environment.NewLine}";
					xml += $"<Bill_First_Name>{billingAddress.FirstName}</Bill_First_Name>{Environment.NewLine}";
					xml += $"<Bill_Last_Name>{billingAddress.LastName}</Bill_Last_Name>{Environment.NewLine}";
					xml += $"<Bill_Address>{billingAddress.Address1}</Bill_Address>{Environment.NewLine}";
					xml += $"<Bill_Zip>{billingAddress.ZipPostalCode}</Bill_Zip>{Environment.NewLine}";
					xml += $"<Company>{domainName}</Company>{Environment.NewLine}";
					xml += $"<email>{billingAddress.Email}</email>{Environment.NewLine}";
					xml += $"</Card_Check>{Environment.NewLine}";
					xml += $"</Request>";

					var webRequest = WebRequest.CreateHttp(AbcConstants.StatusAPIUrl);
					webRequest.Method = "POST";
					webRequest.ContentType = "application/xml";

					byte[] byteArray = Encoding.UTF8.GetBytes(xml);
					webRequest.ContentLength = byteArray.Length;
					using (System.IO.Stream requestStream = webRequest.GetRequestStream())
					{
						requestStream.Write(byteArray, 0, byteArray.Length);
					}

					HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
					System.IO.Stream r_stream = response.GetResponseStream();

					using (System.IO.StreamReader reader = new System.IO.StreamReader(r_stream))
					{
						string strResponse = reader.ReadToEnd();

						if (!string.IsNullOrEmpty(strResponse))
						{
							XmlDocument xmlDoc = new XmlDocument();
							xmlDoc.LoadXml(strResponse);
							
							string xpath = "Response/Card_Check/Resp_Code";
							var response_code = xmlDoc.SelectSingleNode(xpath);
							if (response_code != null)
							{
								status_code = response_code.InnerText;

								xpath = "Response/Card_Check/Resp_Mesg";
								var response_messagenode = xmlDoc.SelectSingleNode(xpath);
								string responseMessageTet = string.Empty;
								if (response_messagenode != null)
								{
									response_message = response_messagenode.InnerText;
								}

								xpath = "Response/Card_Check/Ref_No";
								var ref_nonode = xmlDoc.SelectSingleNode(xpath);								
								if (ref_nonode != null)
								{
                                    HttpContext.Session.Set("Ref_No", ref_nonode.InnerText);
								}

								xpath = "Response/Card_Check/Auth_No";
								var auth_nonode = xmlDoc.SelectSingleNode(xpath);
								if (auth_nonode != null)
								{
                                    HttpContext.Session.Set("Auth_No", auth_nonode.InnerText);
								}
							}
						}
					}
				}
			}
			catch (Exception e)
			{
                _logger.Error("Error occurred when making external payment request. Setting status code to 00 and Ref_No Auth_No to null.", e);
				status_code = "00";
                HttpContext.Session.Set("Ref_No", "");
                HttpContext.Session.Set("Auth_No", "");
			}
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

        [NonAction]
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

        #endregion

        // Makes an external request
        [HttpPost, ActionName("ShippingMethod")]
        [FormValueRequired("nextstep")]
        public IActionResult SelectShippingMethod(string shippingoption, IFormCollection form)
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            if (!_shoppingCartService.ShoppingCartRequiresShipping(cart))
            {
                _genericAttributeService.SaveAttribute<ShippingOption>(_workContext.CurrentCustomer,
                    NopCustomerDefaults.SelectedShippingOptionAttribute, null, _storeContext.CurrentStore.Id);
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
                _genericAttributeService.SaveAttribute<PickupPoint>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPickupPointAttribute, null, _storeContext.CurrentStore.Id);
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
            var shippingOptions = _genericAttributeService.GetAttribute<List<ShippingOption>>(_workContext.CurrentCustomer,
                NopCustomerDefaults.OfferedShippingOptionsAttribute, _storeContext.CurrentStore.Id);
            if (shippingOptions == null || !shippingOptions.Any())
            {
                //not found? let's load them using shipping service
                shippingOptions = _shippingService.GetShippingOptions(cart, _customerService.GetCustomerShippingAddress(_workContext.CurrentCustomer),
                    _workContext.CurrentCustomer, shippingRateComputationMethodSystemName, _storeContext.CurrentStore.Id).ShippingOptions.ToList();
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
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, _storeContext.CurrentStore.Id);

            SendExternalShippingMethodRequest();

            if (_warrantyService.CartContainsWarranties(cart))
            {
                return RedirectToRoute("WarrantySelection");
            }

            return RedirectToRoute("CheckoutPaymentMethod");
        }

        public IActionResult WarrantySelection()
        {
            var cart = _shoppingCartService.GetShoppingCart(
                _workContext.CurrentCustomer,
                ShoppingCartType.ShoppingCart,
                _storeContext.CurrentStore.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            return View(GetWarranties(cart));
        }

        [HttpPost, ActionName("WarrantySelection")]
        [FormValueRequired("nextstep")]
        public IActionResult SelectWarranty(IFormCollection form)
        {
            var cart = _shoppingCartService.GetShoppingCart(
                _workContext.CurrentCustomer,
                ShoppingCartType.ShoppingCart,
                _storeContext.CurrentStore.Id);
            
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
                            _productService.GetProductById(sci.ProductId).Id)
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
                    _genericAttributeService.GetAttribute<List<ShippingOption>>(
                        _workContext.CurrentCustomer,
                        NopCustomerDefaults.OfferedShippingOptionsAttribute,
                        _storeContext.CurrentStore.Id);

                // hold selected shipping option
                var selectedShippingOption =
                    _genericAttributeService.GetAttribute<ShippingOption>(
                        _workContext.CurrentCustomer,
                        NopCustomerDefaults.SelectedShippingOptionAttribute,
                        _storeContext.CurrentStore.Id);

                // hold SelectedPaymentMethod
                //find a selected (previously) payment method
                var selectedPaymentMethodSystemName =
                    _genericAttributeService.GetAttribute<string>(
                        _workContext.CurrentCustomer,
                        NopCustomerDefaults.SelectedPaymentMethodAttribute,
                        _storeContext.CurrentStore.Id);

                // updating shopping cart resets the order state, so we are going to save the state, and re-initialize it after the reset
                _shoppingCartService.UpdateShoppingCartItem(
                    _customerService.GetCustomerById(sci.CustomerId),
                    sci.Id,
                    sci.AttributesXml,
                    sci.CustomerEnteredPrice,
                    sci.RentalStartDateUtc,
                    sci.RentalEndDateUtc,
                    sci.Quantity);

                // re-add shopping cart state to what it was before

                // OfferedShippingOptions
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                                                       NopCustomerDefaults.OfferedShippingOptionsAttribute,
                                                       shippingOptions,
                                                       _storeContext.CurrentStore.Id);

                // SelectedShippingOption
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                    NopCustomerDefaults.SelectedShippingOptionAttribute,
                    selectedShippingOption,
                    _storeContext.CurrentStore.Id);

                // SelectedPaymentMethod
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                                                       NopCustomerDefaults.SelectedPaymentMethodAttribute,
                                                       selectedPaymentMethodSystemName,
                                                       _storeContext.CurrentStore.Id);
            }
        }

        // Custom - uses CC_REF_NO and AUTH_CODE
        [HttpPost, ActionName("Confirm")]
        public IActionResult ConfirmOrder()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            ValidateGiftCardAmounts();

            //model
            var model = _checkoutModelFactory.PrepareConfirmOrderModel(cart);
            try
            {
                //prevent 2 orders being placed within an X seconds time frame
                if (!IsMinimumOrderPlacementIntervalValid(_workContext.CurrentCustomer))
                    throw new Exception(_localizationService.GetResource("Checkout.MinOrderPlacementInterval"));

                //place order
                var processPaymentRequest = HttpContext.Session.Get<ProcessPaymentRequest>("OrderPaymentInfo");
                if (processPaymentRequest == null)
                {
                    //Check whether payment workflow is required
                    if (_orderProcessingService.IsPaymentWorkflowRequired(cart))
                        return RedirectToRoute("CheckoutPaymentInfo");

                    processPaymentRequest = new ProcessPaymentRequest();
                }

                _paymentService.GenerateOrderGuid(processPaymentRequest);
                processPaymentRequest.StoreId = _storeContext.CurrentStore.Id;
                processPaymentRequest.CustomerId = _workContext.CurrentCustomer.Id;
                processPaymentRequest.PaymentMethodSystemName = _genericAttributeService.GetAttribute<string>(_workContext.CurrentCustomer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, _storeContext.CurrentStore.Id);
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
                        return Content(_localizationService.GetResource("Checkout.RedirectMessage"));
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
        public IActionResult EnterPaymentInfo(IFormCollection form)
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

            if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //Check whether payment workflow is required
            var isPaymentWorkflowRequired = _orderProcessingService.IsPaymentWorkflowRequired(cart);
            if (!isPaymentWorkflowRequired)
            {
                return RedirectToRoute("CheckoutConfirm");
            }

            //load payment method
            var paymentMethodSystemName = _genericAttributeService.GetAttribute<string>(_workContext.CurrentCustomer,
                NopCustomerDefaults.SelectedPaymentMethodAttribute, _storeContext.CurrentStore.Id);
            var paymentMethod = _paymentPluginManager
                .LoadPluginBySystemName(paymentMethodSystemName, _workContext.CurrentCustomer, _storeContext.CurrentStore.Id);
            if (paymentMethod == null)
                return RedirectToRoute("CheckoutPaymentMethod");

            var warnings = paymentMethod.ValidatePaymentForm(form);
            foreach (var warning in warnings)
                ModelState.AddModelError("", warning);

            // custom code is here
            string status_code = string.Empty;
            string response_message = string.Empty;
			var isAjaxCall = Convert.ToBoolean(form["is_ajax_call"]);

            if (ModelState.IsValid)
            {
                //get payment info
                var paymentInfo = paymentMethod.GetPaymentInfo(form);
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
            var model = _checkoutModelFactory.PreparePaymentInfoModel(paymentMethod);
            return View(model);
        }

        protected virtual bool ParsePickupInStore(IFormCollection form)
        {
            var pickupInStore = false;

            var pickupInStoreParameter = form["PickupInStore"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(pickupInStoreParameter))
                bool.TryParse(pickupInStoreParameter, out pickupInStore);

            return pickupInStore;
        }


        protected virtual PickupPoint ParsePickupOption(IFormCollection form)
        {
            var pickupPoint = form["pickup-points-id"].ToString().Split(new[] { "___" }, StringSplitOptions.None);
            var pickupPoints = _shippingService.GetPickupPoints(_workContext.CurrentCustomer.BillingAddressId ?? 0,
                _workContext.CurrentCustomer, pickupPoint[1], _storeContext.CurrentStore.Id).PickupPoints.ToList();
            var selectedPoint = pickupPoints.FirstOrDefault(x => x.Id.Equals(pickupPoint[0]));
            if (selectedPoint == null)
                throw new Exception("Pickup point is not allowed");

            return selectedPoint;
        }
        protected virtual void SavePickupOption(PickupPoint pickupPoint)
        {
            var pickUpInStoreShippingOption = new ShippingOption
            {
                Name = string.Format(_localizationService.GetResource("Checkout.PickupPoints.Name"), pickupPoint.Name),
                Rate = pickupPoint.PickupFee,
                Description = pickupPoint.Description,
                ShippingRateComputationMethodSystemName = pickupPoint.ProviderSystemName,
                IsPickupInStore = true
            };
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, pickUpInStoreShippingOption, _storeContext.CurrentStore.Id);
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPickupPointAttribute, pickupPoint, _storeContext.CurrentStore.Id);
        }
	}
}
