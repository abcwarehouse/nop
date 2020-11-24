using Nop.Services.Cms;
using Nop.Services.Plugins;
using System;
using System.Collections.Generic;

namespace Nop.Plugin.Widgets.AbcMarkdownDate
{
    public class MarkdownDateWidget : BasePlugin, IWidgetPlugin
    {
        public bool HideInWidgetList => false;

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "AbcMarkdownDate";
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string> { "productdetails_before_addtocart" };
        }
    }
}
