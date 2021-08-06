using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.Synchrony.Models;
using Nop.Services.Configuration;
using System.Threading.Tasks;

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

        public async Task<ConfigurationModel> ToModelAsync(int storeScope)
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
                model.MerchantId_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.MerchantId, storeScope);
                model.MerchantPassword_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.MerchantPassword, storeScope);
                model.Integration_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.Integration, storeScope);
                model.TokenNumber_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.TokenNumber, storeScope);
                model.WhitelistDomain_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.WhitelistDomain, storeScope);
                model.DemoEndPoint_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.DemoEndPoint, storeScope);
                model.LiveEndPoint_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.LiveEndPoint, storeScope);
                model.ServerURL_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.ServerURL, storeScope);
                model.authorizationEndPoint_Demo_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.authorizationEndPoint_Demo, storeScope);
                model.authorizationEndPoint_Live_OverrideForStore = await settingService.SettingExistsAsync(this, x => x.authorizationEndPoint_Live, storeScope);
                model.IsDebugMode = await settingService.SettingExistsAsync(this, x => x.IsDebugMode, storeScope);
            }

            return model;
        }
    }
}
