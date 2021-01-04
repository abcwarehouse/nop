using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.Synchrony.Models;
using Nop.Services.Configuration;

namespace Nop.Plugin.Payments.Synchrony
{
    public class SynchronyPaymentSettings : ISettings
    {
        public string MerchantId { get; private set; }
        public string MerchantPassword { get; private set; }
        public string TokenNumber { get; private set; }
        public bool Integration { get; private set; }
        public string WhitelistDomain { get; private set; }
        public string DemoEndPoint { get; private set; }
        public string LiveEndPoint { get; private set; }
        public string ServerURL { get; private set; }
        public string authorizationEndPoint_Live { get; private set; }
        public string authorizationEndPoint_Demo { get; private set; }
        public bool IsDebugMode { get; private set; }

        public static SynchronyPaymentSettings FromModel(ConfigurationModel model)
        {
            return new SynchronyPaymentSettings()
            {
                MerchantId = model.MerchantId,
                MerchantPassword = model.MerchantPassword,
                Integration = model.Integration,
                TokenNumber = model.TokenNumber,
                WhitelistDomain = model.WhitelistDomain,
                DemoEndPoint = model.DemoEndPoint,
                LiveEndPoint = model.LiveEndPoint,
                ServerURL = model.ServerURL,
                authorizationEndPoint_Demo = model.authorizationEndPoint_Demo,
                authorizationEndPoint_Live = model.authorizationEndPoint_Live,
                IsDebugMode = model.IsDebugMode
            };
        }

        public static SynchronyPaymentSettings Default()
        {
            return new SynchronyPaymentSettings
            {
                MerchantId = "5348121490000021",
                MerchantPassword = "vsH+/lMYOOTR9fmWJ4gqLg==",
                Integration = true,
                WhitelistDomain = "",
                DemoEndPoint = "https://usvcs.syf.com/v1.0/status/inquiry",
                LiveEndPoint = "https://svcs.syf.com/v1.0/status/inquiry",
                authorizationEndPoint_Demo = "https://ubuy.syf.com/DigitalBuy/authentication.do",
                authorizationEndPoint_Live = "https://buy.syf.com/DigitalBuy/authentication.do"
            };
        }

        public ConfigurationModel ToModel(int storeScope)
        {
            var model = new ConfigurationModel();

            model.MerchantId = MerchantId;
            model.MerchantPassword = MerchantPassword;
            model.Integration = Integration;
            model.TokenNumber = TokenNumber;
            model.WhitelistDomain = WhitelistDomain;
            model.DemoEndPoint = DemoEndPoint;
            model.LiveEndPoint = LiveEndPoint;
            model.ServerURL = ServerURL;
            model.authorizationEndPoint_Demo = authorizationEndPoint_Demo;
            model.authorizationEndPoint_Live = authorizationEndPoint_Live;
            model.IsDebugMode = IsDebugMode;

            model.ActiveStoreScopeConfiguration = storeScope;

            if (storeScope > 0)
            {
                var settingService = EngineContext.Current.Resolve<ISettingService>();
                model.MerchantId_OverrideForStore = settingService.SettingExists(this, x => x.MerchantId, storeScope);
                model.MerchantPassword_OverrideForStore = settingService.SettingExists(this, x => x.MerchantPassword, storeScope);
                model.Integration_OverrideForStore = settingService.SettingExists(this, x => x.Integration, storeScope);
                model.TokenNumber_OverrideForStore = settingService.SettingExists(this, x => x.TokenNumber, storeScope);
                model.WhitelistDomain_OverrideForStore = settingService.SettingExists(this, x => x.WhitelistDomain, storeScope);
                model.DemoEndPoint_OverrideForStore = settingService.SettingExists(this, x => x.DemoEndPoint, storeScope);
                model.LiveEndPoint_OverrideForStore = settingService.SettingExists(this, x => x.LiveEndPoint, storeScope);
                model.ServerURL_OverrideForStore = settingService.SettingExists(this, x => x.ServerURL, storeScope);
                model.authorizationEndPoint_Demo_OverrideForStore = settingService.SettingExists(this, x => x.authorizationEndPoint_Demo, storeScope);
                model.authorizationEndPoint_Live_OverrideForStore = settingService.SettingExists(this, x => x.authorizationEndPoint_Live, storeScope);
                model.IsDebugMode = settingService.SettingExists(this, x => x.IsDebugMode, storeScope);
            }

            return model;
        }
    }
}
