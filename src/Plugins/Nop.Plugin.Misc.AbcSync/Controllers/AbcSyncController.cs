using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AbcSync.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System.Threading.Tasks;

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
        public async Task<ActionResult> Configure(ImportModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            await _settingService.SaveSettingAsync(_importSettings.FromModel(model));

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Admin.Plugins.Saved")
            );

            return Configure();
        }

    }
}
