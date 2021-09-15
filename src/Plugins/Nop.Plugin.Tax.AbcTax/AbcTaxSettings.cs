using Nop.Core.Configuration;

namespace Nop.Plugin.Tax.AbcTax
{
    public class AbcTaxSettings : ISettings
    {
        public string TaxJarAPIKey { get; set; }
    }
}