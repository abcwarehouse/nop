using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.AbcCore.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        public int Priority
        {
            get
            {
                return int.MaxValue;
            }
        }

        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("AbcPromoProductList",
                            "Admin/AbcPromo/Products/{abcPromoId}",
                            new { controller = "AbcPromo", action = "Products", area = "Admin" });

            endpointRouteBuilder.MapControllerRoute("CustomProductEdit",
                            "Admin/Product/Edit/{id}",
                            new { controller = "CustomProduct", action = "Edit", area = "Admin" });

            endpointRouteBuilder.MapControllerRoute("CustomProductEdit",
                            "Admin/Product/Edit",
                            new { controller = "CustomProduct", action = "Edit", area = "Admin" });

            // Add to Cart Slideout
            endpointRouteBuilder.MapControllerRoute("AddToCartSlideout_GetDeliveryOptions",
                            "AddToCart/GetDeliveryOptions",
                            new { controller = "AddToCartSlideout", action = "GetDeliveryOptions"});
        }
    }
}
