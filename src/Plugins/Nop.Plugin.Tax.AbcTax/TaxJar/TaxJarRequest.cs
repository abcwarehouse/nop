using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace Nop.Plugin.Tax.AbcTax.TaxJar
{
    public class TaxJarRequest
    {
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }

        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        [JsonProperty(PropertyName = "street")]
        public string Street { get; set; }

        [JsonProperty(PropertyName = "zip")]
        public string Zip { get; set; }
    }
}
