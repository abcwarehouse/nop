using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.SearchPageRedirect.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Misc.SearchPageRedirect.Controllers
{
    public class SearchPageRedirectController : BasePluginController
    {
        private readonly string RedirectUrlSettingKey = "searchpageredirect.url";
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;

        public SearchPageRedirectController(
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
            var redirectUrl = _settingService.GetSetting(RedirectUrlSettingKey)?.Value;

            var model = new SearchPageRedirectModel();
            model.RedirectUrl = redirectUrl;

            return View("~/Plugins/Misc.SearchPageRedirect/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(SearchPageRedirectModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            _settingService.SetSetting(RedirectUrlSettingKey, model.RedirectUrl);

            _notificationService.SuccessNotification(
                _localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}
