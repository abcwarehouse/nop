using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Autofac;

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
            // builder.RegisterType<AbcMattressPriceListingService>()
            //        .As<IAbcMattressPriceListingService>()
            //        .InstancePerLifetimeScope();
        }

        public int Order
        {
            get { return 2; }
        }
    }
}
