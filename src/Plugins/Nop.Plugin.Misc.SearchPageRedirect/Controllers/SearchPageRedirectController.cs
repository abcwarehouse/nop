using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.SearchPageRedirect.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework.Controllers;
using System.Threading.Tasks;

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

        public async Task<IActionResult> Configure()
        {
            var redirectUrl = (await _settingService.GetSettingAsync(RedirectUrlSettingKey))?.Value;

            var model = new SearchPageRedirectModel();
            model.RedirectUrl = redirectUrl;

            return View("~/Plugins/Misc.SearchPageRedirect/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(SearchPageRedirectModel model)
        {
            if (!ModelState.IsValid)
            {
                return await Configure();
            }

            await _settingService.SetSettingAsync(RedirectUrlSettingKey, model.RedirectUrl);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Admin.Plugins.Saved")
            );

            return await Configure();
        }
    }
}
