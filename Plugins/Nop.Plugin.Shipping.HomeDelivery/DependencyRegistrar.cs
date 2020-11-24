using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Shipping.Fedex;

namespace Nop.Plugin.Shipping.HomeDelivery
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
	{
		/// <summary>
		/// Register services and interfaces
		/// </summary>
		/// <param name="builder">Container builder</param>
		/// <param name="typeFinder">Type finder</param>
		/// <param name="config">Config</param>
		public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
		{
            builder.RegisterType<FedexComputationMethod>()
                .As<FedexComputationMethod>().InstancePerLifetimeScope();
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
