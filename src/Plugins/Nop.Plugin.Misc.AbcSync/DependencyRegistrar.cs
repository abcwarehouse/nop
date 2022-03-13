using Autofac;
using Autofac.Features.ResolveAnything;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using SevenSpikes.Nop.Plugins.StoreLocator.Services;
using Microsoft.Extensions.DependencyInjection;
using Nop.Services.Caching;

namespace Nop.Plugin.Misc.AbcSync
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public void Register(
               IServiceCollection services,
               ITypeFinder typeFinder,
               AppSettings appSettings
        ) {
            services.AddScoped<ArchiveService, ArchiveService>();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 1; }
        }
    }
}
