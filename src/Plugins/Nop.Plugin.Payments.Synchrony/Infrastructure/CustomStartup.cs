using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;

namespace Nop.Plugin.Payments.Synchrony.Infrastructure
{
    public class CustomStartup : INopStartup
    {
        public void ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration
        )
        {
            // To be able to connect to status call, you need TLS 1.3
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls13;
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public int Order
        {
            get { return int.MaxValue; }
        }
    }
}
