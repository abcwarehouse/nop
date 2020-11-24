using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Widgets.AbcEnergyGuide.Services;

namespace Nop.Plugin.Widgets.AbcEnergyGuide.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order => int.MaxValue;

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<ProductEnergyGuideService>().As<IProductEnergyGuideService>();
        }
    }
}
