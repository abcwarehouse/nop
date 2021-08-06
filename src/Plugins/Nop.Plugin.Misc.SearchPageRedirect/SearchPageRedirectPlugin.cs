using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.SearchPageRedirect
{
    public class SearchPageRedirectPlugin : BasePlugin, IMiscPlugin
    {
        private readonly IWebHelper _webHelper;

        public SearchPageRedirectPlugin(
            IWebHelper webHelper
        )
        {
            _webHelper = webHelper;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/SearchPageRedirect/Configure";
        }
    }
}
