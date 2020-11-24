using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.AbcCore;

namespace Nop.Plugin.Misc.AbcSliExport.Infrastructure
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
            builder.RegisterType<SliExportTask>()
                   .As<ISliExportTask>()
                   .InstancePerLifetimeScope();
        }
    }
}
