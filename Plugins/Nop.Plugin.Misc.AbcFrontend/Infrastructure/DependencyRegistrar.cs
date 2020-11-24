using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AbcFrontend.Services;
using Nop.Services.Orders;
using Nop.Services.Tax;

namespace Nop.Plugin.Misc.AbcFrontend.Infrastructure
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
            builder.RegisterType<CustomShoppingCartService>().As<IShoppingCartService>().InstancePerLifetimeScope();
            builder.RegisterType<CustomOrderProcessingService>().As<IOrderProcessingService>();
            builder.RegisterType<WarrantyService>().As<IWarrantyService>();
            builder.RegisterType<CustomTaxService>().As<ICustomTaxService>();
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
