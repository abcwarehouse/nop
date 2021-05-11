using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Autofac;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Nop.Plugin.Misc.AbcMattresses.Factories;
using Nop.Web.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Plugin.Misc.AbcMattresses.Infrastructure
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
            services.AddScoped<IAbcMattressListingPriceService, AbcMattressListingPriceService>();
            services.AddScoped<IAbcMattressModelService, AbcMattressModelService>();
            services.AddScoped<IAbcMattressProductService, AbcMattressProductService>();
            services.AddScoped<IAbcMattressEntryService, AbcMattressEntryService>();
            services.AddScoped<IAbcMattressPackageService, AbcMattressPackageService>();
            services.AddScoped<IAbcMattressBaseService, AbcMattressBaseService>();
            services.AddScoped<IAbcMattressGiftService, AbcMattressGiftService>();
            services.AddScoped<IAbcMattressModelGiftMappingService, AbcMattressModelGiftMappingService>();
            services.AddScoped<IAbcMattressProtectorService, AbcMattressProtectorService>();
            services.AddScoped<IAbcMattressFrameService, AbcMattressFrameService>();
            services.AddScoped<IProductModelFactory, CustomProductModelFactory>();
        }

        public int Order
        {
            get { return 2; }
        }
    }
}
