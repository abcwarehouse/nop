using Nop.Core.Configuration;
using Nop.Plugin.Tax.AbcCountryStateZip.Models;

namespace Nop.Plugin.Tax.AbcCountryStateZip
{
    public class AbcCountyStateZipSettings : ISettings
    {
        public string TaxJarAPIToken { get; private set; }
        public static AbcCountyStateZipSettings FromModel(ConfigurationModel model)
        {
            return new AbcCountyStateZipSettings()
            {
                TaxJarAPIToken = model.TaxJarAPIToken
            };
        }
    }
}
