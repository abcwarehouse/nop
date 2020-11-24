using Nop.Core.Domain;
using Nop.Services.Configuration;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.AbcFrontend
{
    public class FrontEndPlugin : BasePlugin, IPlugin
    {
        private readonly ISettingService _settingService;
        private readonly StoreInformationSettings _storeInformationSettings;

        public FrontEndPlugin(ISettingService settingService, StoreInformationSettings storeInformationSettings)
        {
            _settingService = settingService;
            _storeInformationSettings = storeInformationSettings;
        }

        public override void Install()
        {
            _storeInformationSettings.DefaultStoreTheme = "Pavilion";
            _settingService.SaveSetting(_storeInformationSettings);

            base.Install();
        }
    }

}