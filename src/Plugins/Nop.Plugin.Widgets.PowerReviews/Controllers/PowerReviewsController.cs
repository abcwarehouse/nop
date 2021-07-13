using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AbcCore.Models;
using Nop.Plugin.Widgets.PowerReviews.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Widgets.PowerReviews.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class PowerReviewsController : BasePluginController
    {
        private readonly PowerReviewsSettings _settings;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;

        public PowerReviewsController(PowerReviewsSettings settings,
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
                "~/Plugins/Widgets.PowerReviews/Views/Configure.cshtml",
                _settings.ToModel());
        }

        [HttpPost]
        public ActionResult Configure(PowerReviewsConfigModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            _settingService.SaveSetting(PowerReviewsSettings.FromModel(model));

            _notificationService.SuccessNotification(
                _localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

    }
}
