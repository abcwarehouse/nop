using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AbcFrontend.Services;
using Nop.Services.Orders;
using Nop.Services.Tax;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Plugin.Misc.AbcFrontend.Infrastructure
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public void Register(
               IServiceCollection services,
               ITypeFinder typeFinder,
               AppSettings appSettings
        ) {
            services.AddScoped<ICustomTaxService, CustomTaxService>();
            services.AddScoped<IWarrantyService, WarrantyService>();
            services.AddScoped<IOrderProcessingService, CustomOrderProcessingService>();
            services.AddScoped<IShoppingCartService, CustomShoppingCartService>();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 2; }
        }
    }
}
