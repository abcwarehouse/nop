using Nop.Core;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Plugins;
using System.Collections.Generic;

namespace Nop.Plugin.Widgets.AbcContactUs
{
    public class ContactUsWidget : BasePlugin, IWidgetPlugin
    {
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        public ContactUsWidget(
            ISettingService settingService,
            IWebHelper webHelper
        )
        {
            _settingService = settingService;
            _webHelper = webHelper;
        }

        public override void Install()
        {
            _settingService.SaveSetting(ContactUsWidgetSettings.Default());
            base.Install();
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string> { "topic_page_after_body" };
        }

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "AbcContactUs";
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AbcContactUs/Configure";
        }

        public bool HideInWidgetList => false;
    }
}
