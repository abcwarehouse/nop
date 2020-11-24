using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration
{
    public class AzureWebTestsIntegrationPlugin : BasePlugin, IMiscPlugin
    {
        private readonly IWebHelper _webHelper;

        public AzureWebTestsIntegrationPlugin(
            IWebHelper webHelper
        )
        {
            _webHelper = webHelper;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AzureWebTestsIntegration/Configure";
        }
    }
}
