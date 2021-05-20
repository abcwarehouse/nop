using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AbcCore.HomeDelivery;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcCore.Services.Custom;
using Nop.Plugin.Misc.AbcCore.Data;

namespace Nop.Plugin.Misc.AbcCore.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order
        {
            get
            {
                return int.MaxValue;
            }
        }

        public void Register(
               IServiceCollection services,
               ITypeFinder typeFinder,
               AppSettings appSettings
        ) {
            services.AddScoped<IBackendStockService, BackendStockService>();
            services.AddScoped<IAttributeUtilities, AttributeUtilities>();
            services.AddScoped<FrontEndService>();
            services.AddScoped<IAbcPromoService, AbcPromoService>();
            services.AddScoped<IBaseService, BaseService>();
            services.AddScoped<IIsamGiftCardService, IsamGiftCardService>();
            services.AddScoped<ICustomerShopService, CustomerShopService>();
            services.AddScoped<ICustomShopService, CustomShopService>();
            services.AddScoped<IProductAbcDescriptionService, ProductAbcDescriptionService>();
            services.AddScoped<IHomeDeliveryCostService, HomeDeliveryCostService>();
            services.AddScoped<ICustomProductService, CustomProductService>();
            services.AddScoped<ITermLookupService, TermLookupService>();
            services.AddScoped<ICardCheckService, CardCheckService>();
            services.AddScoped<IProductAbcFinanceService, ProductAbcFinanceService>();
            services.AddScoped<IImportUtilities, ImportUtilities>();
            services.AddScoped<ICustomManufacturerService, CustomManufacturerService>();
            services.AddScoped<ICustomNopDataProvider, CustomMsSqlDataProvider>();
        }
    }
}
