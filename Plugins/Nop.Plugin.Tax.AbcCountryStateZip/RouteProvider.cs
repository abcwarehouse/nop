using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Tax.AbcCountryStateZip
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                "Plugin.Tax.AbcCountryStateZip.AddTaxRate",
                 "Plugins/AbcTaxCountryStateZip/AddTaxRate",
                 new { controller = "AbcTaxCountryStateZip", action = "AddTaxRate" }
            );
            endpointRouteBuilder.MapControllerRoute(
                "Plugin.Tax.AbcCountryStateZip.EnableState",
                 "Plugins/AbcTaxCountryStateZip/EnableState",
                 new { controller = "AbcTaxCountryStateZip", action = "EnableState" }
            );
        }

        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
