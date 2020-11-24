using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AzureWebTestsIntegration.Services;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration.Infrastructure
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
            builder.RegisterType<PluginSettingService>().As<IPluginSettingService>().InstancePerLifetimeScope();
            builder.RegisterType<AzureWebTestIntegrationService>().As<IAzureWebTestIntegrationService>().InstancePerLifetimeScope();
        }
    }
}
