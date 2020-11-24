using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Widgets.AbcBonusBundle.Services;

namespace Nop.Plugin.Widgets.AbcBonusBundle.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order => int.MaxValue;

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<ProductAbcBundleService>().As<IProductAbcBundleService>();
        }
    }
}
