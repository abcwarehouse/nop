﻿using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.EnhancedLogging.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.EnhancedLogging.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class EnhancedLoggingController : BasePluginController
    {
        private readonly EnhancedLoggingSettings _settings;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;

        public EnhancedLoggingController(
            EnhancedLoggingSettings settings,
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
                "~/Plugins/Misc.EnhancedLogging/Views/Configure.cshtml",
                _settings.ToModel());
        }

        [HttpPost]
        public IActionResult Configure(EnhancedLoggingConfigModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            _settingService.SaveSetting(
                EnhancedLoggingSettings.FromModel(model));

            _notificationService.SuccessNotification(
                _localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}
