using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Web.Framework.Components;
using System.Threading.Tasks;

namespace AbcWarehouse.Plugin.Widgets.AddToCartSlideout.Components
{
    public class AddToCartSlideoutSubtotalViewComponent : NopViewComponent
    {
        private readonly IPriceFormatter _priceFormatter;
        
        public AddToCartSlideoutSubtotalViewComponent(
            IPriceFormatter priceFormatter
        ) {
            _priceFormatter = priceFormatter;
        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, decimal price)
        {
            return View("~/Plugins/Widgets.AddToCartSlideout/Views/_Subtotal.cshtml", await _priceFormatter.FormatPriceAsync(price));
        }
    }
}
