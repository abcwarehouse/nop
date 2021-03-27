using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AbcCore.HomeDelivery;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcCore.Services.Custom;
using Nop.Plugin.Widgets.AbcSynchronyPayments.Services;

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

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<BackendStockService>()
                   .As<IBackendStockService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<ImportUtilities>()
                   .As<IImportUtilities>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<AttributeUtilities>()
                   .As<IAttributeUtilities>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<FrontEndService>().As<FrontEndService>().SingleInstance();
            builder.RegisterType<AbcPromoService>().As<IAbcPromoService>().InstancePerLifetimeScope();
            builder.RegisterType<BaseService>().As<IBaseService>().InstancePerLifetimeScope();
            builder.RegisterType<IsamGiftCardService>().As<IIsamGiftCardService>().InstancePerLifetimeScope();
            builder.RegisterType<CustomerShopService>().As<ICustomerShopService>().InstancePerLifetimeScope();
            builder.RegisterType<CustomShopService>().As<ICustomShopService>();
            builder.RegisterType<ProductAbcDescriptionService>().As<IProductAbcDescriptionService>();
            builder.RegisterType<HomeDeliveryCostService>()
                   .As<IHomeDeliveryCostService>();
            builder.RegisterType<CustomProductService>()
                   .As<ICustomProductService>();
            builder.RegisterType<TermLookupService>()
                   .As<ITermLookupService>();
            builder.RegisterType<CardCheckService>()
                 .As<ICardCheckService>();
            builder.RegisterType<ProductAbcFinanceService>()
                   .As<IProductAbcFinanceService>();
        }
    }
}
