using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync
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
            services.AddScoped<IProductDataProductService, ProductDataProductService>();
            services.AddScoped<IProductDataProductDimensionService, ProductDataProductDimensionService>();
            services.AddScoped<IProductDataProductDownloadService, ProductDataProductDownloadService>();
            services.AddScoped<IProductDataProductFeatureService, ProductDataProductFeatureService>();
            services.AddScoped<IProductDataProductFilterService, ProductDataProductFilterService>();
            services.AddScoped<IProductDataProductImageService, ProductDataProductImageService>();
            services.AddScoped<IProductDataProductpmapService, ProductDataProductpmapService>();
            services.AddScoped<IProductDataProductRelatedItemService, ProductDataProductRelatedItemService>();
            services.AddScoped<ISiteOnTimeBrandService, SiteOnTimeBrandService>();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 1; }
        }
    }
}
