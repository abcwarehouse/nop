using Newtonsoft.Json;
using Nop.Plugin.Misc.AzureWebTestsIntegration.Domain;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration.DTOs
{
    public class GetWebTestsResponse
    {
        [JsonProperty("value")]
        public AzureWebTest[] Value { get; set; }
    }
}
