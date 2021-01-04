using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Autofac;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Nop.Plugin.Misc.AbcMattresses.Factories;
using Nop.Web.Factories;

namespace Nop.Plugin.Misc.AbcMattresses.Infrastructure
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(
            ContainerBuilder builder,
            ITypeFinder typeFinder,
            NopConfig config
        )
        {
            builder.RegisterType<AbcMattressListingPriceService>()
                   .As<IAbcMattressListingPriceService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<AbcMattressModelService>()
                   .As<IAbcMattressModelService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<AbcMattressProductService>()
                   .As<IAbcMattressProductService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<AbcMattressEntryService>()
                   .As<IAbcMattressEntryService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<AbcMattressPackageService>()
                   .As<IAbcMattressPackageService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<AbcMattressBaseService>()
                   .As<IAbcMattressBaseService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<AbcMattressGiftService>()
                   .As<IAbcMattressGiftService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<AbcMattressModelGiftMappingService>()
                   .As<IAbcMattressModelGiftMappingService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<AbcMattressProtectorService>()
                   .As<IAbcMattressProtectorService>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<CustomProductModelFactory>()
                   .As<IProductModelFactory>()
                   .InstancePerLifetimeScope();
        }

        public int Order
        {
            get { return 2; }
        }
    }
}
