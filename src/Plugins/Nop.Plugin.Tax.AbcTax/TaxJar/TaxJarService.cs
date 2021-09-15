using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Nop.Plugin.Tax.AbcTax.TaxJar
{
    public class TaxJarService : ITaxJarService
    {
        public async Task<TaxJarResponse> GetTaxJarRateAsync(TaxJarRequest request)
        {
            return new TaxJarResponse();
        }
    }
}

