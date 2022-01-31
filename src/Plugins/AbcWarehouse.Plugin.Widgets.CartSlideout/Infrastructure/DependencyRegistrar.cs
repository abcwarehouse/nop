using AbcWarehouse.Plugin.Widgets.CartSlideout.Services;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Web.Factories;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Infrastructure
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

        public void Register(
               IServiceCollection services,
               ITypeFinder typeFinder,
               AppSettings appSettings)
        {
            services.AddScoped<IAbcDeliveryService, AbcDeliveryService>();
        }
    }
}
