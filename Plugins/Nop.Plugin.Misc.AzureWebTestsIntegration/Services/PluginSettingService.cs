using Nop.Plugin.Misc.AzureWebTestsIntegration.Models;
using Nop.Services.Configuration;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration.Services
{
    internal class PluginSettingService : IPluginSettingService
    {
        private readonly string ClientIdSettingKey = "azurewebtestsintegration.clientid";
        private readonly string ClientSecretSettingKey = "azurewebtestsintegration.clientsecret";
        private readonly string TenantIdSettingKey = "azurewebtestsintegration.tenantid";

        private readonly ISettingService _settingService;
        public PluginSettingService(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public SettingsModel GetSettingsModel()
        {
            var clientId = _settingService.GetSetting(ClientIdSettingKey)?.Value;
            var clientSecret = _settingService.GetSetting(ClientSecretSettingKey)?.Value;
            var tenantId = _settingService.GetSetting(TenantIdSettingKey)?.Value;

            return new SettingsModel
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                TenantId = tenantId
            };
        }

        public void SaveSettingsModel(SettingsModel model)
        {
            _settingService.SetSetting(ClientIdSettingKey, model.ClientId);
            _settingService.SetSetting(ClientSecretSettingKey, model.ClientSecret);
            _settingService.SetSetting(TenantIdSettingKey, model.TenantId);
        }
    }
}
