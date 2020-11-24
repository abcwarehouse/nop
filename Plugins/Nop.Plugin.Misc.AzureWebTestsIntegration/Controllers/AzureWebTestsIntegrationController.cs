using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AzureWebTestsIntegration.Models;
using Nop.Plugin.Misc.AzureWebTestsIntegration.Services;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class AzureWebTestsIntegrationController : BasePluginController
    {
        private readonly IPluginSettingService _pluginSettingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;

        public AzureWebTestsIntegrationController(
            IPluginSettingService pluginSettingService,
            ILocalizationService localizationService,
            INotificationService notificationService)
        {
            _pluginSettingService = pluginSettingService;
            _localizationService = localizationService;
            _notificationService = notificationService;
        }

        public ActionResult Configure()
        {
            var model = _pluginSettingService.GetSettingsModel();

            return View(
                "~/Plugins/Misc.AzureWebTestsIntegration/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public ActionResult Configure(SettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            _pluginSettingService.SaveSettingsModel(model);
            _notificationService.SuccessNotification(
                _localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}
