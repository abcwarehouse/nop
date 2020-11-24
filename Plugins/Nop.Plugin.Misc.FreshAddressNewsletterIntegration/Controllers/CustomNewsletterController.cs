using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Plugin.Misc.FreshAddressNewsletterIntegration.Services;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System;

namespace Nop.Plugin.Misc.FreshAddressNewsletterIntegration.Controllers
{
    /* hooks into the Newsletter route to override functionality */
    public class CustomNewsletterController : BasePublicController
    {
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IStoreContext _storeContext;
        private readonly CustomerSettings _customerSettings;
        private readonly ILogger _logger;
        private readonly IFreshAddressService _freshAddressService;

        public CustomNewsletterController(ILocalizationService localizationService,
            IWorkContext workContext,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            IWorkflowMessageService workflowMessageService,
            IStoreContext storeContext,
            CustomerSettings customerSettings,
            ILogger logger,
            IFreshAddressService freshAddressService)
        {
            _localizationService = localizationService;
            _workContext = workContext;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _workflowMessageService = workflowMessageService;
            _storeContext = storeContext;
            _customerSettings = customerSettings;
            _logger = logger;
            _freshAddressService = freshAddressService;
        }

        //available even when a store is closed
        [CheckAccessClosedStore(true)]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual IActionResult SubscribeNewsletter(string email, bool subscribe)
        {
            string result;
            var success = false;

            if (!CommonHelper.IsValidEmail(email))
            {
                result = _localizationService.GetResource("Newsletter.Email.Wrong");
            }
            else
            {
                email = email.Trim();

                var subscription = _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(email, _storeContext.CurrentStore.Id);
                if (subscription != null)
                {
                    if (subscribe)
                    {
                        if (!subscription.Active)
                        {
                            _workflowMessageService.SendNewsLetterSubscriptionActivationMessage(subscription, _workContext.WorkingLanguage.Id);
                        }
                        result = _localizationService.GetResource("Newsletter.SubscribeEmailSent");
                    }
                    else
                    {
                        if (subscription.Active)
                        {
                            _workflowMessageService.SendNewsLetterSubscriptionDeactivationMessage(subscription, _workContext.WorkingLanguage.Id);
                        }
                        result = _localizationService.GetResource("Newsletter.UnsubscribeEmailSent");
                    }
                }
                else if (subscribe)
                {
                    try
                    {
                        var freshAddressResponse = _freshAddressService.ValidateEmail(email);
                        if (!freshAddressResponse.IsValid)
                        {
                            return Json(new
                            {
                                Success = false,
                                Result = freshAddressResponse.ErrorResponse,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"An error occured while trying to validate a Newsletter signup through FreshAddress: {ex}");
                    }

                    subscription = new NewsLetterSubscription
                    {
                        NewsLetterSubscriptionGuid = Guid.NewGuid(),
                        Email = email,
                        Active = false,
                        StoreId = _storeContext.CurrentStore.Id,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    _newsLetterSubscriptionService.InsertNewsLetterSubscription(subscription);
                    _workflowMessageService.SendNewsLetterSubscriptionActivationMessage(subscription, _workContext.WorkingLanguage.Id);

                    result = _localizationService.GetResource("Newsletter.SubscribeEmailSent");
                }
                else
                {
                    result = _localizationService.GetResource("Newsletter.UnsubscribeEmailSent");
                }
                success = true;
            }

            return Json(new
            {
                Success = success,
                Result = result,
            });
        }
    }
}
