using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Controllers
{
    [Area(AreaNames.Admin)]
    public class AbcSiteOnTimeSyncController : BasePluginController
    {
        private readonly AbcSiteOnTimeSyncSettings _importSettings;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;

        public AbcSiteOnTimeSyncController(AbcSiteOnTimeSyncSettings importSettings,
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

        [AuthorizeAdmin]
        [AutoValidateAntiforgeryToken]
        public ActionResult Configure()
        {
            return View("~/Plugins/Misc.AbcSiteOnTimeSync/Views/Configure.cshtml", _importSettings.ToModel());
        }

        [HttpPost]
        [AuthorizeAdmin]
        [AutoValidateAntiforgeryToken]
        public ActionResult Configure(ConfiguationModel model)
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

        [HttpPost]
        public async Task<IActionResult> VerifySettingsAsync(
            [FromBody] VerifySettingsModel model
        )
        {
            var url = model.CmicApiBrandUrl;
            Uri.TryCreate(model.CmicApiBrandUrl, UriKind.Absolute, out var validUrl);
            if (validUrl == null)
            {
                return BadRequest("Invalid URL provided.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Basic", Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                            $"{model.CmicApiUsername}:{model.CmicApiPassword}")));

                using (HttpResponseMessage res = await client.GetAsync(url))
                {
                    switch (res.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            return Unauthorized("Check credentials.");
                        case HttpStatusCode.OK:
                            return Ok();
                        // if we get an unexpected response code
                        default:
                            return StatusCode(500);
                    }
                }
            }
        }
    }
}
