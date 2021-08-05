using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nop.Plugin.Widgets.Bronto.Models;
using System.Threading.Tasks;

namespace Nop.Plugin.Widgets.Bronto.Controllers
{
    [Area(AreaNames.Admin)]
    [AuthorizeAdmin]
    [AutoValidateAntiforgeryToken]
    public class BrontoController : BasePluginController
    {
        private readonly BrontoSettings _settings;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;

        public BrontoController(
            BrontoSettings settings,
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

        public IActionResult Configure()
        {
            var model = _settings.ToModel();

            return View("~/Plugins/Widgets.Bronto/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            await _settingService.SaveSettingAsync(BrontoSettings.FromModel(model));
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return View("~/Plugins/Widgets.Bronto/Views/Configure.cshtml", model);
        }
    }
}
