using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Factories;
using Nop.Web.Framework.Components;
using System.Threading.Tasks;

namespace AbcWarehouse.Plugin.Widgets.AddToCartSlideout.Components
{
    public class AddToCartSlideoutProductAttributesViewComponent : NopViewComponent
    {
        private readonly IAbcProductModelFactory _productModelFactory;
        
        public AddToCartSlideoutProductAttributesViewComponent(
            IAbcProductModelFactory productModelFactory
        ) {
            _productModelFactory = productModelFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync(Product product)
        {
            var models = await _productModelFactory.PrepareProductAttributeModelsAsync(
                product,
                null,
                new string[] {
                    "Delivery/Pickup Options",
                    "Haul Away"
                }    
            );

            return View("~/Plugins/Misc.AbcFrontend/Views/Product/_ProductAttributes.cshtml", models);
        }
    }
}
