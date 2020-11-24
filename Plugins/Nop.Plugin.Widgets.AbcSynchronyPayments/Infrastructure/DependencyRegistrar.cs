using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Widgets.AbcSynchronyPayments.Services;

namespace Nop.Plugin.Widgets.AbcSynchronyPayments.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order => int.MaxValue;

        public void Register(
            ContainerBuilder builder,
            ITypeFinder typeFinder,
            NopConfig config
        )
        {
            builder.RegisterType<ProductAbcFinanceService>()
                   .As<IProductAbcFinanceService>();
        }
    }
}
