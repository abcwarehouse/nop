using Nop.Core.Infrastructure.DependencyManagement;
using System;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Autofac;
using Nop.Plugin.Misc.AbcExportOrder.Services;
using Nop.Services.Orders;

namespace Nop.Plugin.Misc.AbcExportOrder
{
    class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order { get { return 1; } }

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<CustomShoppingCartService>().As<IShoppingCartService>().InstancePerLifetimeScope();
            builder.RegisterType<IsamOrderService>().As<IIsamOrderService>().InstancePerLifetimeScope();
            builder.RegisterType<CustomOrderService>().As<ICustomOrderService>().InstancePerLifetimeScope();
        }
    }
}
