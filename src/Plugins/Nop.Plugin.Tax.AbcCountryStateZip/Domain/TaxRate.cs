using Nop.Core;

namespace Nop.Plugin.Tax.AbcCountryStateZip.Domain
{
    public partial class TaxRate : BaseEntity
    {
        public int StoreId { get; set; }
        public int TaxCategoryId { get; set; }
        public int CountryId { get; set; }
        public int StateProvinceId { get; set; }
        public string ZipCode { get; set; }
        public decimal Percentage { get; set; }
        public bool EnableTaxState { get; set; }
    }
}