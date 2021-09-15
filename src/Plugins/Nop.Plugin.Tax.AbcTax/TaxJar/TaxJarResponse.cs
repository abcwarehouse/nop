using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace Nop.Plugin.Tax.AbcTax.TaxJar
{
    public class TaxJarResponse
    {
        [JsonProperty(PropertyName = "rate")]
        public TaxJarRate Rate { get; set; }

        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        [JsonProperty(PropertyName = "detail")]
        public string ErrorDetails { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string ErrorStatus { get; set; }

        public bool IsSuccess
        {
            get { return string.IsNullOrEmpty(Error); }
        }

        public string ErrorMessage
        {
            get { return IsSuccess ? string.Empty : string.Format("{0} - {1} ({2})", ErrorStatus, Error, ErrorDetails); }
        }
    }
}
