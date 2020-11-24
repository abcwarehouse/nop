using Nop.Services.Cms;
using Nop.Services.Plugins;
using System.Collections.Generic;
using Nop.Web.Framework.Infrastructure;
using Nop.Plugin.Misc.AbcCore.Infrastructure;

namespace Nop.Plugin.Widgets.AbcPromoBanners
{
    public class AbcSynchronyPaymentsPlugin : BasePlugin, IWidgetPlugin
    {
        public bool HideInWidgetList => false;

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "AbcSynchronyPayments";
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string>
            {
                PublicWidgetZones.ProductBoxAddinfoMiddle,
                CustomPublicWidgetZones.ProductDetailsAfterPrice
            };
        }
    }
}
