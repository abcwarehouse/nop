using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AbcSync.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.AbcSync.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class AbcSyncController : BasePluginController
    {
        private readonly ImportSettings _importSettings;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;

        public AbcSyncController(ImportSettings importSettings,
            ISettingService settingService,
            ILocalizationService localizationService,
            INotificationService notificationService
        )
        {
            _importSettings = importSettings;
            _settingService = settingService;
            _localizationService = localizationService;
            _notificationService = notificationService;
        }

        public ActionResult Configure()
        {
            return View("~/Plugins/Misc.AbcSync/Views/Configure.cshtml", _importSettings.ToModel());
        }

        [HttpPost]
        public ActionResult Configure(ImportModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            _settingService.SaveSetting(_importSettings.FromModel(model));

            _notificationService.SuccessNotification(
                _localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

    }
}
