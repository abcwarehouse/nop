using System;
using System.Collections.Generic;
using Nop.Core;
using Nop.Plugin.Misc.AbcCore.Infrastructure;
using Nop.Plugin.Misc.AbcFrontend.Infrastructure;
using Nop.Services.Cms;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Widgets.PowerReviews
{
    public class PowerReviewsPlugin : BasePlugin, IWidgetPlugin
    {
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;

        public PowerReviewsPlugin(
            ILocalizationService localizationService,
            IWebHelper webHelper
        )
        {
            _localizationService = localizationService;
            _webHelper = webHelper;
        }

        public bool HideInWidgetList => false;

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PowerReviews/Configure";
        }

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "WidgetsPowerReviews";
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string>
            {
                //PublicWidgetZones.ProductBoxAddinfoBefore,
                CustomPublicWidgetZones.ProductBoxAddinfoReviews,
                //PublicWidgetZones.ProductDetailsOverviewTop,
                CustomPublicWidgetZones.ProductDetailsReviews,
                CustomPublicWidgetZones.ProductDetailsReviewsTab,
                CustomPublicWidgetZones.ProductDetailsReviewsTabContent,

                // standard - used for listing script
                PublicWidgetZones.CategoryDetailsBottom,
                PublicWidgetZones.ManufacturerDetailsBottom
            };
        }

        public override void Update(string currentVersion, string targetVersion)
        {
            _localizationService.AddPluginLocaleResource(
                new Dictionary<string, string>
                {
                    [PowerReviewsLocales.APIKey] = "API Key",
                    [PowerReviewsLocales.APIKeyHint] = "API key provided by PowerReviews.",
                    [PowerReviewsLocales.MerchantGroupId] = "Merchant Group ID",
                    [PowerReviewsLocales.MerchantGroupIdHint] = "Merchant Group ID provided by PowerReviews.",
                    [PowerReviewsLocales.MerchantId] = "Merchant ID",
                    [PowerReviewsLocales.MerchantIdHint] = "Merchant ID provided by PowerReviews.",
                }
            );

            base.Update(currentVersion, targetVersion);
        }
    }
}
