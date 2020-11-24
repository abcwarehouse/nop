using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.AbcCard.Controllers;
using Nop.Plugin.Payments.AbcCard.Models;
using Nop.Plugin.Payments.AbcCard.Validators;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;

namespace Nop.Plugin.Payments.AbcCard
{
    /// <summary>
    /// AbcCard payment processor
    /// </summary>
    public class AbcCardPaymentProcessor : BasePlugin, IPaymentMethod
    {
        private readonly AbcCardPaymentSettings _AbcCardPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IPaymentService _paymentService;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;

        public AbcCardPaymentProcessor(
            AbcCardPaymentSettings AbcCardPaymentSettings,
            ISettingService settingService, 
            IOrderTotalCalculationService orderTotalCalculationService,
            IShoppingCartService shoppingCartService,
            IPaymentService paymentService,
            ILocalizationService localizationService,
            IWebHelper webHelper
        )
        {
            _AbcCardPaymentSettings = AbcCardPaymentSettings;
            _settingService = settingService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _shoppingCartService = shoppingCartService;
            _paymentService = paymentService;
            _localizationService = localizationService;
            _webHelper = webHelper;
        }
        
        #region Methods
        
        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            result.AllowStoringCreditCardNumber = true;
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country

            if (_AbcCardPaymentSettings.ShippableProductRequired &&
                !_shoppingCartService.ShoppingCartRequiresShipping(cart))
                return true;

            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = _paymentService.CalculateAdditionalFee(
                cart,
                _AbcCardPaymentSettings.AdditionalFee,
                _AbcCardPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //it's not a redirection payment method. So we always return false
            return false;
        }

        public Type GetControllerType()
        {
            return typeof(PaymentAbcCardController);
        }

        public override void Install()
        {
            //settings
            var settings = new AbcCardPaymentSettings
            {
                DescriptionText = "<p>Enter your card information</p>"
            };
            _settingService.SaveSetting(settings);

            //locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.Payment.AbcCard.DescriptionText"] = "Description",
                ["Plugins.Payment.AbcCard.DescriptionText.Hint"] = "Enter info that will be shown to customers during checkout",
                ["Plugins.Payment.AbcCard.AdditionalFee"] = "Additional fee",
                ["Plugins.Payment.AbcCard.AdditionalFee.Hint"] = "The additional fee.",
                ["Plugins.Payment.AbcCard.AdditionalFeePercentage"] = "Additional fee. Use percentage",
                ["Plugins.Payment.AbcCard.AdditionalFeePercentage.Hint"] = "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
                ["Plugins.Payment.AbcCard.ShippableProductRequired"] = "Shippable product required",
                ["Plugins.Payment.AbcCard.ShippableProductRequired.Hint"] = "An option indicating whether shippable products are required in order to display this payment method during checkout.",
            });
            
            base.Install();
        }
        
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<AbcCardPaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.Payment.AbcCard");
            
            base.Uninstall();
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            //TODO validate card info
            var warnings = new List<string>();
            var cardNumber = form["CardNumber"];

            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardNumber = form["CardNumber"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                    warnings.Add(error.ErrorMessage);
            return warnings;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CreditCardNumber = form["CardNumber"];
            return paymentInfo;
        }

        public string GetPublicViewComponentName()
        {
            return "AbcCard";
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentAbcCard/Configure";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Standard;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }

        public string PaymentMethodDescription => "Allows for payment via ABC Warehouse card.";

        #endregion

    }
}
