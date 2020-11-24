﻿using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.AbcExportOrder.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
	public class AbcExportOrderController : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly ExportOrderSettings _settings;
		private readonly INotificationService _notificationService;

        public AbcExportOrderController(
            ISettingService settingService,
            ILocalizationService localizationService,
            ExportOrderSettings settings,
			INotificationService notificationService
		)
        {
            _settingService = settingService;
            _localizationService = localizationService;
            _settings = settings;
			_notificationService = notificationService;
        }

		public IActionResult Configure()
		{
			return View("~/Plugins/Misc.AbcExportOrder/Views/Configure.cshtml", _settings.ToModel());
		}

		[HttpPost]
		public IActionResult Configure(ConfigModel model)
		{
			if (!ModelState.IsValid)
			{
				return Configure();
			}

			_settingService.SaveSetting(ExportOrderSettings.FromModel(model));

			_notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

			return Configure();
		}
	}
}
