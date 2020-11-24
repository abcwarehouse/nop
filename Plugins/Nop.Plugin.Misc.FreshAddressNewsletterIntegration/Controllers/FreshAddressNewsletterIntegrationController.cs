using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.FreshAddressNewsletterIntegration.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.FreshAddressNewsletterIntegration.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class FreshAddressNewsletterIntegrationController : BasePluginController
    {
        private readonly string CompanyIdSettingKey = "freshaddressintegration.companyid";
        private readonly string ContractIdSettingKey = "freshaddressintegration.contractid";
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;

        public FreshAddressNewsletterIntegrationController(
            ILocalizationService localizationService,
            ISettingService settingService,
            INotificationService notificationService
        )
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _notificationService = notificationService;
        }

        public IActionResult Configure()
        {
            var companyId = _settingService.GetSetting(CompanyIdSettingKey)?.Value;
            var contractId = _settingService.GetSetting(ContractIdSettingKey)?.Value;

            var model = new FreshAddressNewsletterIntegrationModel();
            model.CompanyId = companyId;
            model.ContractId = contractId;
            
            return View(
                "~/Plugins/Misc.FreshAddressNewsletterIntegration/Views/Configure.cshtml",
                model);
        }

        [HttpPost]
        public IActionResult Configure(FreshAddressNewsletterIntegrationModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            _settingService.SetSetting(CompanyIdSettingKey, model.CompanyId);
            _settingService.SetSetting(ContractIdSettingKey, model.ContractId);

            _notificationService.SuccessNotification(
                _localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}
