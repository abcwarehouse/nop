using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
	{
		/// <summary>
		/// Register services and interfaces
		/// </summary>
		/// <param name="builder">Container builder</param>
		/// <param name="typeFinder">Type finder</param>
		/// <param name="config">Config</param>
		public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
		{
            builder.RegisterType<ProductDataProductService>().As<IProductDataProductService>();
            builder.RegisterType<ProductDataProductDimensionService>().As<IProductDataProductDimensionService>();
            builder.RegisterType<ProductDataProductDownloadService>().As<IProductDataProductDownloadService>();
			builder.RegisterType<ProductDataProductFeatureService>().As<IProductDataProductFeatureService>();
			builder.RegisterType<ProductDataProductFilterService>().As<IProductDataProductFilterService>();
			builder.RegisterType<ProductDataProductImageService>().As<IProductDataProductImageService>();
			builder.RegisterType<ProductDataProductpmapService>().As<IProductDataProductpmapService>();
			builder.RegisterType<ProductDataProductRelatedItemService>().As<IProductDataProductRelatedItemService>();
			builder.RegisterType<SiteOnTimeBrandService>().As<ISiteOnTimeBrandService>();
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
