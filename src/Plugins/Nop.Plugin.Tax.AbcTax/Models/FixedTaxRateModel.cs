using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Tax.AbcTax.Models
{
    public record FixedTaxRateModel : BaseNopModel
    {
        public int TaxCategoryId { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcTax.Fields.TaxCategoryName")]
        public string TaxCategoryName { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcTax.Fields.Rate")]
        public decimal Rate { get; set; }
    }
}