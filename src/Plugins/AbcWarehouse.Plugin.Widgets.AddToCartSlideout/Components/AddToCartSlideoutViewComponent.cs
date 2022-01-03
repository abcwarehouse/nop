using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace AbcWarehouse.Plugin.Widgets.AddToCartSlideout.Components
{
    public class AddToCartSlideoutViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke(string widgetZone, object additionalData = null)
        {
            return View("~/Plugins/Widgets.AddToCartSlideout/Views/Slideout.cshtml");
        }
    }
}
