using Autofac;
using EllaSoftware.Plugin.Misc.CronTasks.Services;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Quartz;
using Quartz.Impl;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EllaSoftware.Plugin.Misc.CronTasks.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        // public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        // {
        //     builder.RegisterType<CronTaskService>().As<ICronTaskService>().InstancePerLifetimeScope();

        //     //quartz
        //     builder.RegisterType<StdSchedulerFactory>().SingleInstance();
        //     builder.Register(context => context.Resolve<StdSchedulerFactory>().GetScheduler().Result)
        //         .As<IScheduler>()
        //         .SingleInstance();
        // }

        public void Register(
               IServiceCollection services,
               ITypeFinder typeFinder,
               AppSettings appSettings
        ) {
            services.AddScoped<ICronTaskService, CronTaskService>();

            // quartz
            services.AddSingleton<StdSchedulerFactory>();
            throw new Exception("need to fix issue with singletons first");
            // services.AddSingleton<StdSchedulerFactory, (context => context.Resolve<StdSchedulerFactory>().GetScheduler().Result)>();
        }

        public int Order
        {
            get { return 1; }
        }
    }
}
