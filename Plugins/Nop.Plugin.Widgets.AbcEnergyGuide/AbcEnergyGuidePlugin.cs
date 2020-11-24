using System.Collections.Generic;
using Nop.Services.Cms;
using Nop.Services.Plugins;
using Nop.Plugin.Misc.AbcCore.Infrastructure;

namespace Nop.Plugin.Widgets.AbcEnergyGuide
{
    public class AbcEnergyGuidePlugin : BasePlugin, IWidgetPlugin
    {
        public IList<string> GetWidgetZones()
        {
            return new List<string> { CustomPublicWidgetZones.ProductDetailsDescriptionTabTop };
        }

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "WidgetsAbcEnergyGuide";
        }

        public bool HideInWidgetList => false;
    }
}