using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Nop.Plugin.Tax.AbcTax.TaxJar
{
    public interface ITaxJarService
    {
        public Task<TaxJarResponse> GetTaxJarRateAsync(TaxJarRequest request);
    }
}
