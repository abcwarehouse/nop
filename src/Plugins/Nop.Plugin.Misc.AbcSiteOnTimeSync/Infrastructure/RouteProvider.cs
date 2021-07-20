using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Infrastructure
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
            endpointRouteBuilder.MapControllerRoute(
                "SiteOnTimeSyncVerifySettings",
                "Admin/AbcSiteOnTimeSync/VerifySettings",
                new
                {
                    controller = "AbcSiteOnTimeSync",
                    action = "VerifySettingsAsync"
                }
            );
        }
    }
}
