﻿using Nop.Core.Infrastructure.DependencyManagement;
using System;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Autofac;
using Nop.Plugin.Misc.AbcExportOrder.Services;
using Nop.Services.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Plugin.Misc.AbcExportOrder
{
    class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order { get { return 1; } }

        public void Register(
               IServiceCollection services,
               ITypeFinder typeFinder,
               AppSettings appSettings
        ) {
            services.AddScoped<CustomShoppingCartService, IShoppingCartService>();
            services.AddScoped<IsamOrderService, IIsamOrderService>();
            services.AddScoped<CustomOrderService, ICustomOrderService>();
            services.AddScoped<YahooService, IYahooService>();
        }
    }
}
