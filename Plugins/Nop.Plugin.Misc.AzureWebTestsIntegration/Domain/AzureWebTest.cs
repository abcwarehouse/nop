using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration.Domain
{
    public class AzureWebTest
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("properties")]
        public WebTestProperties Properties { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, string> Tags { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
