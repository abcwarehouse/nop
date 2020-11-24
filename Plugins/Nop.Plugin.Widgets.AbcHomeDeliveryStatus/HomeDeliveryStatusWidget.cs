using Nop.Services.Cms;
using Nop.Services.Plugins;
using System.Collections.Generic;

namespace Nop.Plugin.Widgets.AbcHomeDeliveryStatus
{
    public class HomeDeliveryStatusWidget : BasePlugin, IWidgetPlugin
    {
        public bool HideInWidgetList => false;

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "WidgetsAbcHomeDeliveryStatus";
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string> { "topic_page_after_body" };
        }
    }
}
