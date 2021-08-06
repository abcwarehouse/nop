using Nop.Web.Framework;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Tax.AbcCountryStateZip.Models
{
    public record TaxRateModel : BaseNopModel
    {
        /* This needs to be mapped to controller */
        public int Id { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Store")]
        public int StoreId { get; set; }
        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Store")]
        public string StoreName { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.TaxCategory")]
        public int TaxCategoryId { get; set; }
        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.TaxCategory")]
        public string TaxCategoryName { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Country")]
        public int CountryId { get; set; }
        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Country")]
        public string CountryName { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.StateProvince")]
        public int StateProvinceId { get; set; }
        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.StateProvince")]
        public string StateProvinceName { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Zip")]
        public string Zip { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Percentage")]
        public decimal Percentage { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.EnableTaxState")]
        public bool EnableTaxState { get; set; }
    }
}