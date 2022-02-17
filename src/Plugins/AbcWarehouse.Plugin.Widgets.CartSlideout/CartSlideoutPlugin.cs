using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Tasks;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Plugins;
using Nop.Services.Tasks;
using Nop.Web.Framework.Infrastructure;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout
{
    public class CartSlideoutPlugin : BasePlugin, IWidgetPlugin
    {
        public bool HideInWidgetList => false;

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "CartSlideout";
        }

        public System.Threading.Tasks.Task<IList<string>> GetWidgetZonesAsync()
        {
            return System.Threading.Tasks.Task.FromResult<IList<string>>(new List<string> { PublicWidgetZones.BodyStartHtmlTagAfter });
        }
    }
}
