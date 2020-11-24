using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Tax.AbcCountryStateZip.Models
{
    public class ConfigurationModel : BaseSearchModel
    {
        public ConfigurationModel()
        {
            AvailableStores = new List<SelectListItem>();
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
            AvailableTaxCategories = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Store")]
        public int AddStoreId { get; set; }
        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Country")]
        public int AddCountryId { get; set; }
        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.StateProvince")]
        public int AddStateProvinceId { get; set; }
        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Zip")]
        public string AddZip { get; set; }
        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.TaxCategory")]
        public int AddTaxCategoryId { get; set; }
        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.Percentage")]
        public decimal AddPercentage { get; set; }

        [NopResourceDisplayName("Plugins.Tax.AbcCountryStateZip.Fields.TaxJarAPIToken")]
        public string TaxJarAPIToken { get; set; }

        public IList<SelectListItem> AvailableStores { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }
        public IList<SelectListItem> AvailableStates { get; set; }
        public IList<SelectListItem> AvailableTaxCategories { get; set; }
    }
}