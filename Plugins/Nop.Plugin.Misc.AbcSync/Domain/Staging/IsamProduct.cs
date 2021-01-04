using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSync.Domain.Staging
{
    public class IsamProduct
    {
        public int Id { get; set; }
        public string ItemNumber { get; set; }
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string Color { get; set; }
        public bool OnAbcSite { get; set; }
        public bool OnHawthorneSite { get; set; }
        public bool OnAbcClearanceSite { get; set; }
        public bool? OnHawthorneClearanceSite { get; set; }
        public string Sku { get; set; }
        public string ManufacturerNumber { get; set; }
        public bool DisableBuying { get; set; }
        public decimal Weight { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public bool AllowInStorePickup { get; set; }
        public int InstockFlag { get; set; }
        public bool IsNew { get; set; }
        public DateTime? NewExpectedDate { get; set; }
        public DateTime? LimitedStockDate { get; set; }
        public decimal BasePrice { get; set; }
        public decimal DisplayPrice { get; set; }
        public decimal CartPrice { get; set; }
        public bool UsePairPricing { get; set; }
        public int PriceBucketCode { get; set; }
        public bool CanUseUps { get; set; }
        public bool CustomerEntersPrice { get; set; }
        public string Upc { get; set; }
        public string FactTag { get; set; }
    }
}
