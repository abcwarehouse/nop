using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Widgets.PowerReviews.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        public int Priority => 100;

        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("WriteAReview",
                            "write-a-review",
                            new { controller = "PowerReviews", action = "WriteAReview" });
        }
    }
}
