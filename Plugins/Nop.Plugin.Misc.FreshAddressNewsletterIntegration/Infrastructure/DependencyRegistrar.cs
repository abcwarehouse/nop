using Nop.Core.Infrastructure.DependencyManagement;
using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.FreshAddressNewsletterIntegration.Services;

namespace Nop.Plugin.Misc.FreshAddressNewsletterIntegration.Infrastructure
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
            builder.RegisterType<FreshAddressService>().As<IFreshAddressService>().InstancePerLifetimeScope();
        }
    }
}
