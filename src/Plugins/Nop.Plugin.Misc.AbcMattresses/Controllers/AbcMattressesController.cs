using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AbcCore.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Plugin.Misc.AbcMattresses.Models;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcMattresses.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class AbcMattressesController : BasePluginController
    {
        private readonly AbcMattressesSettings _settings;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;

        public AbcMattressesController(AbcMattressesSettings settings,
            ISettingService settingService,
            ILocalizationService localizationService,
            INotificationService notificationService
        )
        {
            _settings = settings;
            _settingService = settingService;
            _localizationService = localizationService;
            _notificationService = notificationService;
        }

        public ActionResult Configure()
        {
            return View(
                "~/Plugins/Misc.AbcMattresses/Views/Configure.cshtml",
                _settings.ToModel());
        }

        [HttpPost]
        public async Task<ActionResult> Configure(AbcMattressesConfigModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            await _settingService.SaveSettingAsync(AbcMattressesSettings.FromModel(model));

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}
