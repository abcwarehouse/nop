using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration.Services
{
    public interface IAzureWebTestIntegrationService
    {
        void EnableWebTest(string subscriptionId, string resourceGroup, string webTestName);

        void DisableWebTest(string subscriptionId, string resourceGroup, string webTestName);
    }
}
