using System;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcCore.Infrastructure;
using Nop.Plugin.Misc.AbcFrontend.Infrastructure;
using Nop.Services.Cms;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Widgets.PowerReviews
{
    public class PowerReviewsPlugin : BasePlugin, IWidgetPlugin
    {
        public bool HideInWidgetList => false;

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
    }
}
