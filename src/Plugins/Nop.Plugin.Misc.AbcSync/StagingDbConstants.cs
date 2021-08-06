namespace Nop.Plugin.Misc.AbcSync
{
    public class StagingDbConstants
    {
        public static readonly string ProductTable = "dbo.Product";
        public static readonly string ItemNumber = "ItemNumber";
        public static readonly string ProductBasePrice = "BasePrice";
        public static readonly string ProductDisplayPrice = "DisplayPrice";
        public static readonly string ProductCartPrice = "CartPrice";
        public static readonly string ProductPairPricing = "UsePairPricing";
        public static readonly string ProductPriceBucket = "PriceBucketCode";
        public static readonly string ProductSku = "Sku";

        public static readonly string PrFileDiscountsTable = "dbo.PrFileDiscounts";
        public static readonly string PrFileDiscountsSku = "ProductSku";
        public static readonly string PrFileDiscountsOnAbc = "IsAbcDiscount";
        public static readonly string PrFileDiscountsOnHawthorne = "IsHawthorneDiscount";
        public static readonly string PrFileDiscountsName = "Name";
        public static readonly string PrFileDiscountsAmount = "DiscountAmount";
        public static readonly string PrFileDiscountsStartDate = "StartDate";
        public static readonly string PrFileDiscountsEndDate = "EndDate";

        public static readonly string ManufacturerTable = "dbo.Manufacturer";
        public static readonly string ManufacturerCode = "BrandCode";
        public static readonly string ManufacturerName = "Name";
        public static readonly string ManufacturerOnAbc = "OnAbcSite";
        public static readonly string ManufacturerOnHawthorne = "OnHawthorneSite";
        public static readonly string ManufacturerOnClearance = "OnAbcClearanceSite";

        public static readonly string MappingTable = "dbo.ProductManufacturerMapping";
        public static readonly string MappingItemSku = "ItemSku";
        public static readonly string MappingBrand = "BrandCode";

        public static readonly string ProdCatMappingTable = "dbo.ProductCategoryMapping";
        public static readonly string ProdCatMappingItemSku = "ItemSku";
        public static readonly string ProdCatMappingCategoryAbcId = "CategoryAbcId";

        public static readonly string ProdFeatureTable = "dbo.ProductFeature";
        public static readonly string ProdFeatureSku = "Sku";
        public static readonly string ProdFeatureDisplayOrder = "DisplayOrder";
        public static readonly string ProdFeatureText = "FeatureText";

        public static readonly string ScandownTable = "dbo.ScandownDates";
        public static readonly string ScandownSku = "Sku";
        public static readonly string ScandownStartDate = "StartDate";
        public static readonly string ScandownEndDate = "EndDate";

        public static readonly string WarrantyItemTable = "WarrantyItem";
        public static readonly string WarrantyItemName = "Name";
        public static readonly string WarrantyItemPriceAdjustment = "PriceAdjustment";
        public static readonly string WarrantyItemWarrantyGroupCode = "WarrantyGroupCode";
        public static readonly string WarrantyItemWarrantySku = "WarrantyItemSku";

        public static readonly string WarrantyGroupTable = "WarrantyGroup";
        public static readonly string WarrantyGroupWarrantyGroupCode = "WarrantyGroupCode";
        public static readonly string WarrantyGroupName = "Description";

        public static readonly string ProductWarrantyGroupMappingTable = "ProductWarrantyGroupMapping";
        public static readonly string ProductWarrantyGroupWarrantyGroupCode = "WarrantyGroupCode";
        public static readonly string ProductWarrantyGroupProductSku = "ProductSku";

        public static readonly string RebateTable = "dbo.Rebate";
        public static readonly string RebateBrand = "AbcBrand";
        public static readonly string RebateAbcId = "AbcId";
        public static readonly string RebateName = "Name";
        public static readonly string RebateAmount = "RebateAmount";
        public static readonly string RebateStartDate = "StartDate";
        public static readonly string RebateEndDate = "EndDate";

        public static class Promo
        {
            public static readonly string Table = "dbo.Promo";
            public static readonly string AbcBuyer = "AbcBuyerId";
            public static readonly string AbcDepartment = "AbcDeptCode";
            public static readonly string AbcCode = "AbcPromoCode";
            public static readonly string Name = "Name";
            public static readonly string StartDate = "StartDate";
            public static readonly string EndDate = "EndDate";
            public static readonly string IsPercantage = "DiscountUsesPercentage";
            public static readonly string Amount = "DiscountAmount";
            public static readonly string Percentage = "DiscountPercentage";
        }

        public static class PromoProductMapping
        {
            public static readonly string Table = "dbo.PromoProductMapping";
            public static readonly string AbcBuyer = "AbcBuyerId";
            public static readonly string AbcDepartment = "AbcDeptCode";
            public static readonly string AbcCode = "AbcPromoCode";
            public static readonly string ItemSku = "ItemSku";
        }

        public static class RebateProductMapping
        {
            public static readonly string Table = "dbo.RebateProductMapping";
            public static readonly string Brand = "AbcBuyerId";
            public static readonly string RebateId = "AbcDeptCode";
            public static readonly string ItemSku = "AbcPromoCode";
        }
    }
}