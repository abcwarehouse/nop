using Autofac;
using Autofac.Features.ResolveAnything;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AbcSync.Data;
using Nop.Plugin.Misc.AbcSync.Services;
using Nop.Plugin.Misc.AbcSync.Services.Staging;
using SevenSpikes.Nop.Plugins.StoreLocator.Services;
using Microsoft.Extensions.DependencyInjection;
using Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate;

namespace Nop.Plugin.Misc.AbcSync
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
            services.AddScoped<ArchiveService, ArchiveService>();
            services.AddScoped<IImportService, ImportService>();
            services.AddScoped<IImportMarkdowns, ImportMarkdowns>();
            services.AddScoped<IImportRelatedProducts, ImportRelatedProducts>();
            services.AddScoped<IImportIsamSpecs, ImportIsamSpecs>();
            services.AddScoped<IImportProductFlags, ImportProductFlags>();
            services.AddScoped<IDocumentImportService, DocumentImportService>();
            services.AddScoped<IImportPictureService, ImportPictureService>();

            services.AddScoped<ISiteOnTimeProductService, SiteOnTimeProductService>();
            services.AddScoped<IPrFileDiscountService, PrFileDiscountService>();
            services.AddScoped<IPromoService, PromoService>();
            services.AddScoped<IRebateService, RebateService>();
            services.AddScoped<IRebateProductMappingService, RebateProductMappingService>();
            services.AddScoped<IProductDataProductService, ProductDataProductService>();
            services.AddScoped<IProductDataProductImageService, ProductDataProductImageService>();
            services.AddScoped<IProductDataProductDownloadService, ProductDataProductDownloadService>();
            services.AddScoped<IIsamProductService, IsamProductService>();
            services.AddScoped<ICustomCategoryService, CustomCategoryService>();

            services.AddScoped<StagingDb, StagingDb>();

            // needed to allow for calling tasks in DI
            services.AddScoped<ImportProductsTask, ImportProductsTask>();
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
