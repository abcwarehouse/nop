using System.Xml.Linq;
using System.Data.Common;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Data;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Domain.Localization;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.FreshAddressNewsletterIntegration.Infrastructure
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
            var pattern = string.Empty;
            if (DataSettingsManager.DatabaseIsInstalled)
            {
                var localizationSettings = endpointRouteBuilder.ServiceProvider.GetRequiredService<LocalizationSettings>();
                if (localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    var langservice = endpointRouteBuilder.ServiceProvider.GetRequiredService<ILanguageService>();
                    var languages = langservice.GetAllLanguages().ToList();
                    pattern = "{language:lang=" + languages.FirstOrDefault().UniqueSeoCode + "}/";
                }
            }

            endpointRouteBuilder.MapControllerRoute(
                "CustomSubscribeNewsletter",
                 $"{pattern}subscribenewsletter",
                 new { controller = "CustomNewsletter", action = "SubscribeNewsletter" }
            );
        }
    }
}
