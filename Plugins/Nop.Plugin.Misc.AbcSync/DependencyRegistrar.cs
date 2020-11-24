using Autofac;
using Autofac.Features.ResolveAnything;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AbcSync.Data;
using Nop.Plugin.Misc.AbcSync.Services;
using Nop.Plugin.Misc.AbcSync.Services.Staging;
using SevenSpikes.Nop.Plugins.StoreLocator.Services;

namespace Nop.Plugin.Misc.AbcSync
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
            builder.RegisterType<ArchiveService>()
                .As<ArchiveService>().InstancePerLifetimeScope();
            builder.RegisterType<ImportService>()
				.As<IImportService>().InstancePerLifetimeScope();
            builder.RegisterType<ImportService>()
                .As<ImportService>().InstancePerLifetimeScope();
            builder.RegisterType<ImportMarkdowns>()
                .As<IImportMarkdowns>().InstancePerLifetimeScope();
            builder.RegisterType<ImportRelatedProducts>()
                .As<IImportRelatedProducts>().InstancePerLifetimeScope();
            builder.RegisterType<ImportIsamSpecs>()
				.As<IImportIsamSpecs>().InstancePerLifetimeScope();
            builder.RegisterType<ImportIsamSpecs>()
                .As<ImportIsamSpecs>().InstancePerLifetimeScope();
			builder.RegisterType<ImportProductFlags>()
				.As<IImportProductFlags>().InstancePerLifetimeScope();
            builder.RegisterType<ShopService>()
                .As<IShopService>().InstancePerLifetimeScope();
            builder.RegisterType<DocumentImportService>()
                .As<IDocumentImportService>().InstancePerLifetimeScope();
            builder.RegisterType<ImportPictureService>()
                .As<IImportPictureService>().InstancePerLifetimeScope();
            builder.RegisterType<GoogleMapsGeocodingService>().As<IGeocodingService>();

            // 4.3 Staging
            builder.RegisterType<SiteOnTimeProductService>().As<ISiteOnTimeProductService>();
            builder.RegisterType<PrFileDiscountService>().As<IPrFileDiscountService>();
            builder.RegisterType<PromoService>().As<IPromoService>();
            builder.RegisterType<RebateService>().As<IRebateService>();
            builder.RegisterType<RebateProductMappingService>().As<IRebateProductMappingService>();
            builder.RegisterType<ProductDataProductService>().As<IProductDataProductService>();
            builder.RegisterType<ProductDataProductImageService>().As<IProductDataProductImageService>();
            builder.RegisterType<ProductDataProductDownloadService>().As<IProductDataProductDownloadService>();
            builder.RegisterType<IsamProductService>().As<IIsamProductService>();
            builder.RegisterType<CustomCategoryService>().As<ICustomCategoryService>();

            builder.RegisterType<StagingDb>().As<StagingDb>();

            builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource(x => x.Name.Contains("Task")));
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
