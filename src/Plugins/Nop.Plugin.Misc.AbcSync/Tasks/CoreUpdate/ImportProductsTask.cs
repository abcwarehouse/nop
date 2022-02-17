using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tax;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcCore.Mattresses;
using Nop.Plugin.Misc.AbcSync.Data;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using Nop.Plugin.Misc.AbcSync.Extensions;
using Nop.Plugin.Misc.AbcSync.Services.Staging;
using Nop.Plugin.Misc.AbcSync.Staging;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nop.Plugin.Misc.AbcCore.Services.Custom;
using Nop.Plugin.Misc.AbcCore.Data;

namespace Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate
{
    public partial class ImportProductsTask : IScheduleTask
    {
        private readonly ImportSettings _importSettings;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<TaxCategory> _taxCategoryRepository;
        private readonly ICustomNopDataProvider _nopDbContext;
        private readonly IImportUtilities _importUtilities;
        private readonly IRepository<ProductCartPrice> _productCartPriceRepository;
        private readonly IStoreService _storeService;
        private readonly IRepository<ProductAttributeMapping> _productAttributeMappingRepository;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IRepository<Manufacturer> _manufacturerRepository;
        private readonly ILogger _logger;
        private readonly ICustomManufacturerService _manufacturerService;
        private readonly IPrFileDiscountService _prFileDiscountService;
        private readonly IRepository<ProductManufacturer> _productManufacturerRepository;
        private readonly IAbcMattressProductService _abcMattressProductService;
        private readonly FrontEndService _frontendService;

        private readonly StagingDb _stagingDb;

        private EntityManager<ProductCartPrice> _productCartPriceManager;
        private EntityManager<ProductHomeDelivery> _homeDeliveryManager = new EntityManager<ProductHomeDelivery>();
        private EntityManager<ProductRequiresLogin> _requiresLoginManager = new EntityManager<ProductRequiresLogin>();
        private EntityManager<ProductAttributeMapping> _productAttributeMappingManager = 
            new EntityManager<ProductAttributeMapping>(EngineContext.Current.Resolve<IRepository<ProductAttributeMapping>>());

        private ProductAttribute _homeDeliveryAttribute;
        private HashSet<int> _productIdsWithHomeDeliveryAttribute;
        private HashSet<int> _productIdsWithPlaceholderDescriptions;
        private Dictionary<string, int> _existingSkuToId;
        private Dictionary<string, decimal> _skuToPrDiscount;
        private int _everythingTaxCategoryId;

        private Store _abcWarehouseStore;
        private Store _abcClearanceStore;
        private Store _hawthorneStore;
        private Store _hawthorneClearanceStore;

        public ImportProductsTask(
            ImportSettings importSettings,
            IRepository<Product> productRepository,
            IRepository<TaxCategory> taxCategoryRepository,
            ICustomNopDataProvider nopDbContext,
            IImportUtilities importUtilities,
            IRepository<ProductCartPrice> productCartPriceRepository,
            IStoreService storeService,
            IRepository<ProductAttributeMapping> productAttributeMappingRepository,
            IUrlRecordService urlRecordService,
            IProductService productService,
            IProductAttributeService productAttributeService,
            IStoreMappingService storeMappingService,
            IGenericAttributeService genericAttributeService,
            IRepository<Manufacturer> manufacturerRepository,
            ILogger logger,
            ICustomManufacturerService manufacturerService,
            StagingDb stagingDb,
            IPrFileDiscountService prFileDiscountService,
            IRepository<ProductManufacturer> productManufacturerRepository,
            IAbcMattressProductService abcMattressProductService,
            FrontEndService frontendService
        )
        {
            _importSettings = importSettings;
            _productRepository = productRepository;
            _taxCategoryRepository = taxCategoryRepository;
            _nopDbContext = nopDbContext;
            _importUtilities = importUtilities;
            _productCartPriceRepository = productCartPriceRepository;
            _storeService = storeService;
            _productAttributeMappingRepository = productAttributeMappingRepository;
            _urlRecordService = urlRecordService;
            _productService = productService;
            _productAttributeService = productAttributeService;
            _storeMappingService = storeMappingService;
            _genericAttributeService = genericAttributeService;
            _manufacturerRepository = manufacturerRepository;
            _logger = logger;
            _manufacturerService = manufacturerService;
            _stagingDb = stagingDb;
            _prFileDiscountService = prFileDiscountService;
            _productManufacturerRepository = productManufacturerRepository;
            _abcMattressProductService = abcMattressProductService;
            _frontendService = frontendService;
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            _homeDeliveryAttribute = await _importUtilities.GetHomeDeliveryAttributeAsync();
            _productIdsWithHomeDeliveryAttribute = new HashSet<int>(_productAttributeMappingRepository.Table
                .Where(pam => pam.ProductAttributeId == _homeDeliveryAttribute.Id).Select(pam => pam.ProductId).Distinct().ToArray());

            _existingSkuToId = _productRepository.Table.Select(p => new { p.Sku, p.Id }).ToDictionary(p => p.Sku, p => p.Id);

            _productCartPriceManager = new EntityManager<ProductCartPrice>(_productCartPriceRepository);

            Store[] storeList = (await _storeService.GetAllStoresAsync()).ToArray();
            _abcWarehouseStore
                = storeList.Where(s => s.Name == "ABC Warehouse")
                .Select(s => s).FirstOrDefault();
            _abcClearanceStore
                = storeList.Where(s => s.Name == "ABC Clearance")
                .Select(s => s).FirstOrDefault();
            _hawthorneStore
                = storeList.Where(s => s.Name == "Hawthorne Online Store")
                .Select(s => s).FirstOrDefault();
            _hawthorneClearanceStore
                = storeList.Where(s => s.Name == "Hawthorne Clearance")
                .Select(s => s).FirstOrDefault();

            var validDiscounts = _prFileDiscountService.GetPrFileDiscounts();
            if (_abcWarehouseStore != null)
            {
                _skuToPrDiscount = validDiscounts.Where(prd => prd.IsAbcDiscount).OrderBy(prd => prd.ProductSku).ToDictionary(prd => prd.ProductSku, prd => prd.DiscountAmount);
            }
            else if (_hawthorneStore != null)
            {
                _skuToPrDiscount = validDiscounts.Where(prd => prd.IsHawthorneDiscount).OrderBy(prd => prd.ProductSku).ToDictionary(prd => prd.ProductSku, prd => prd.DiscountAmount);
            }

            _productIdsWithPlaceholderDescriptions = new HashSet<int>(_productRepository.Table.Where(p => p.FullDescription.Contains("placeholder-features")).Select(p => p.Id).ToArray());
        
            _everythingTaxCategoryId = _taxCategoryRepository.Table.Where(tc => tc.Name == "Everything").Select(tc => tc.Id).FirstOrDefault();
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportProductsTask)
            {
                this.Skipped();
                return;
            }
            await InitializeAsync();

            var stagingDb = _importSettings.GetStagingDbConnection();

            //remove all product cart price mappings and home delivery mappings
            await _nopDbContext.ExecuteNonQueryAsync($"DELETE FROM ProductCartPrice");
            await _nopDbContext.ExecuteNonQueryAsync($"DELETE FROM ProductHomeDelivery");
            await _nopDbContext.ExecuteNonQueryAsync($"DELETE FROM ProductRequiresLogin");

            var stagingProducts = await GetCleanedStagingProductsAsync();

            ProductAttribute homeDeliveryAttribute = await _importUtilities.GetHomeDeliveryAttributeAsync();
            PredefinedProductAttributeValue homeDeliveryAttributeValue = await _importUtilities.GetHomeDeliveryAttributeValueAsync();
            ProductAttribute pickupAttribute = await _importUtilities.GetPickupAttributeAsync();

            // set all manufacturers to limit to store
            string manufacturerUpdateQuery
                = $"UPDATE {_nopDbContext.GetTable<Manufacturer>().TableName} set LimitedToStores = 1;";
            // delete all store mappings & manufacturer store mappings for all products
            string manufacturerStoreMappingDeleteQuery =
                $"DELETE FROM {_nopDbContext.GetTable<StoreMapping>().TableName} WHERE EntityName='Manufacturer'";

            await _nopDbContext.ExecuteNonQueryAsync(manufacturerUpdateQuery);
            await _nopDbContext.ExecuteNonQueryAsync(manufacturerStoreMappingDeleteQuery);

            Store[] storeList = (await _storeService.GetAllStoresAsync()).ToArray();
            Store abcWarehouseStore
                = storeList.Where(s => s.Name == "ABC Warehouse")
                .Select(s => s).FirstOrDefault();
            Store abcClearanceStore
                = storeList.Where(s => s.Name == "ABC Clearance")
                .Select(s => s).FirstOrDefault();
            Store hawthorneStore
                = storeList.Where(s => s.Name == "Hawthorne Online Store")
                .Select(s => s).FirstOrDefault();
            Store hawthorneClearanceStore
                = storeList.Where(s => s.Name == "Hawthorne Clearance")
                .Select(s => s).FirstOrDefault();

            var productsWithPickupAttribute = new HashSet<int>(_productAttributeMappingRepository.Table
                .Where(pam => pam.ProductAttributeId == pickupAttribute.Id).Select(pam => pam.ProductId).Distinct().ToArray());

            foreach (var stagingProduct in stagingProducts)
            {
                await ProcessStagingProductAsync(stagingProduct);
            }

            await ImportProductStoreMappingsAsync();

            await _productAttributeMappingManager.FlushAsync();
            await _productCartPriceManager.FlushAsync();
            await _homeDeliveryManager.FlushAsync();
            await _requiresLoginManager.FlushAsync();

            // marking products that do not exist in the staging database as deleted
            var nopDbName = DataSettingsManager.LoadSettings().ConnectionString.GetDatabaseName();
            var stagingDbName = stagingDb.Database;
            var deleteCommand = $@"
                update[{nopDbName}].dbo.Product set Published = 0, Deleted = 1
                from [{nopDbName}].[dbo].[Product] 
                left join[{stagingDbName}].[dbo].[ProductSource] as ps on ([{nopDbName}].[dbo].[Product].[Sku] = ps.IsamSku)
                where ps.IsamSku is null
                AND Product.Id NOT IN (SELECT ProductId FROM [AbcMattressModel])
            ";

            await _nopDbContext.ExecuteNonQueryAsync(deleteCommand, 60);

            //updating gift card if one was added
            var giftCardProduct = _productRepository.Table.Where(p => p.Name == "GIFT").FirstOrDefault();
            if (giftCardProduct != null)
            {
                giftCardProduct.Deleted = false;
                giftCardProduct.Published = true;
                giftCardProduct.LimitedToStores = false;
                giftCardProduct.CustomerEntersPrice = true;
                giftCardProduct.MinimumCustomerEnteredPrice = 25;
                giftCardProduct.MaximumCustomerEnteredPrice = 1000;
                await _productService.UpdateProductAsync(giftCardProduct);
            }

            //update the warranty product
            var warrantyProduct = _importUtilities.GetExistingProductBySku("WARRPLACE_SKU");
            int warrantyTaxCategoryId = _taxCategoryRepository.Table.Where(tc => tc.Name == "Warranties").Select(tc => tc.Id).FirstOrDefault();
            if (warrantyProduct != null)
            {
                warrantyProduct.Deleted = false;
                warrantyProduct.Published = false;
                warrantyProduct.LimitedToStores = false;
                warrantyProduct.TaxCategoryId = warrantyTaxCategoryId;
                await _productService.UpdateProductAsync(warrantyProduct);
            }

            //unpublish any manufacturers that now contain only deleted products
            await _nopDbContext.ExecuteNonQueryAsync(
                $"update [{nopDbName}].dbo.Manufacturer set Published = 0; update [{nopDbName}].dbo.Manufacturer set Published = 1 from [{nopDbName}].dbo.Manufacturer m join [{nopDbName}].dbo.Product_Manufacturer_Mapping pm on m.Id = pm.ManufacturerId left join  (select Id as pid from [{nopDbName}].dbo.Product p where p.Published = 1 or p.Deleted = 0 ) as p on pm.ProductId = p.pid;");

            //clear table and reimport
            await _nopDbContext.ExecuteNonQueryAsync(
                $@"TRUNCATE TABLE ProductAbcDescriptions;
                    INSERT INTO ProductAbcDescriptions (Product_Id, AbcDescription, AbcItemNumber, UsesPairPricing)
                    SELECT p.Id, sp.ShortDescription, sp.ItemNumber, sp.UsePairPricing FROM Product p join {stagingDb.Database}.dbo.Product sp on p.Sku = sp.Sku;"
                );

            // handle Bronto descriptions
            await _nopDbContext.ExecuteNonQueryAsync(
                $@"DELETE FROM GenericAttribute
                    WHERE KeyGroup = 'Product'
                    AND [Key] = 'BrontoDescription'"
                );

            await _nopDbContext.ExecuteNonQueryAsync(
                $@"INSERT INTO GenericAttribute (EntityId, KeyGroup, [Key], Value, StoreId, CreatedOrUpdatedDateUTC)
                    SELECT p.Id, 'Product', 'BrontoDescription', sp.ShortDescription, 0, GETUTCDATE()
                    FROM Product p
                    JOIN {stagingDb.Database}.dbo.Product sp on p.Sku = sp.Sku
                    WHERE sp.ShortDescription is not null"
                );

            // handle PowerReviews descriptions
            await _nopDbContext.ExecuteNonQueryAsync(
                $@"DELETE FROM GenericAttribute
                    WHERE KeyGroup = 'Product'
                    AND [Key] = 'PowerReviewsDescription'"
                );

            await _nopDbContext.ExecuteNonQueryAsync(
                $@"INSERT INTO GenericAttribute (EntityId, KeyGroup, [Key], Value, StoreId, CreatedOrUpdatedDateUTC)
                    SELECT p.Id, 'Product', 'PowerReviewsDescription', sp.ShortDescription, 0, GETUTCDATE()
                    FROM Product p
                    JOIN {stagingDb.Database}.dbo.Product sp on p.Sku = sp.Sku
                    WHERE sp.ShortDescription is not null"
                );
        }

        private async System.Threading.Tasks.Task SetFullDescriptionIfEmptyAsync(StagingProduct stagingProduct, Product product)
        {
            if (string.IsNullOrWhiteSpace(product.FullDescription))
            {
                if (!string.IsNullOrWhiteSpace(stagingProduct.FactTag))
                {
                    await _logger.WarningAsync(
                        $"Product {product.Sku} has no FullDescription, using Fact Tag {stagingProduct.FactTag}");
                    product.FullDescription = stagingProduct.FactTag;
                }
                else
                {
                    await _logger.WarningAsync(
                        $"Product {product.Sku} has no FullDescription, setting blank");
                    product.FullDescription = "<div></div>";
                }

                await _productService.UpdateProductAsync(product);
            }
        }

        private async System.Threading.Tasks.Task UpdateAddToCartInfoAsync(
            PriceBucketCode priceBucketCode,
            Product product,
            StagingProduct stagingProduct)
        {
            if (priceBucketCode == PriceBucketCode.AddToCartForCurrentPricing)
            {
                await product.EnableAddToCartAsync();

                product.Price = stagingProduct.CartPrice.Value;
                product.OldPrice = 0.00M;
                await _productService.UpdateProductAsync(product);
            }
            else if (await product.IsAddToCartAsync())
            {
                await product.DisableAddToCartAsync();
            }

            if (priceBucketCode == PriceBucketCode.AddToCartForCurrentPricingRequireLogin)
            {
                await product.EnableAddToCartWithUserInfoAsync();

                product.Price = stagingProduct.CartPrice.Value;
                product.OldPrice = 0.00M;
                await _productService.UpdateProductAsync(product);
            }
            else if (await product.IsAddToCartWithUserInfoAsync())
            {
                await product.DisableAddToCartWithUserInfoAsync();
            }
        }

        private async System.Threading.Tasks.Task ImportProductStoreMappingsAsync()
        {
            var stagingDbName = _importSettings.GetStagingDbConnection().Database;
            var nopDbName = DataSettingsManager.LoadSettings().ConnectionString.GetDatabaseName();
            var command = $@"
                DROP TABLE IF EXISTS #tmp_products
                DROP TABLE IF EXISTS #tmp_mappings

                -- Clear all current product mappings
                DELETE FROM {nopDbName}.dbo.StoreMapping
                WHERE EntityName = 'Product'
                AND EntityId NOT IN (SELECT ProductId FROM [AbcMattressModel])

                -- Gets a full list of ABC items from staging DB
                SELECT DISTINCT
	                p.Id,
	                p.Name,
	                p.ShortDescription,
	                p.OnAbcSite,
	                p.OnHawthorneSite,
	                p.OnAbcClearanceSite,
	                p.OnHawthorneClearanceSite,
	                p.Sku,
	                p.ManufacturerNumber,
	                m.Name as Manufacturer,
	                p.DisableBuying,
	                p.[Weight],
	                p.[Length],
	                p.Width,
	                p.Height,
	                p.AllowInStorePickup,
	                p.IsNew,
	                p.NewExpectedDate,
	                p.LimitedStockDate,
	                p.BasePrice,
                    p.DisplayPrice,
                    p.CartPrice,
                    p.UsePairPricing,
                    p.PriceBucketCode,
	                p.CanUseUps,
	                p.ItemNumber as [ISAMItemNo],
	                p.Upc,
	                p.FactTag
	                INTO #tmp_products
	                FROM {stagingDbName}.dbo.[Product] as p
	                LEFT OUTER JOIN {stagingDbName}.dbo.ProductManufacturerMapping ON Sku = ItemSku
	                LEFT OUTER JOIN {stagingDbName}.dbo.Manufacturer as m ON ProductManufacturerMapping.BrandCode = m.BrandCode

                -- Find the NOPCommerce store IDs
                DECLARE @abcStoreId INT
                SELECT @abcStoreId = Id FROM {nopDbName}.dbo.Store WHERE Name LIKE '%ABC%' AND Name NOT LIKE '%Clearance%'
                DECLARE @abcClearanceStoreId INT
                SELECT @abcClearanceStoreId = Id FROM {nopDbName}.dbo.Store WHERE Name LIKE '%ABC%' AND Name LIKE '%Clearance%'
                DECLARE @hawthorneStoreId INT
                SELECT @hawthorneStoreId = Id FROM {nopDbName}.dbo.Store WHERE Name LIKE '%Hawthorne%' AND Name NOT LIKE '%Clearance%'
                DECLARE @hawthorneClearanceStoreId INT
                SELECT @hawthorneClearanceStoreId = Id FROM {nopDbName}.dbo.Store WHERE Name LIKE '%Hawthorne%' AND Name LIKE '%Clearance%'

                -- Create mappings table
                SELECT p.Id, OnAbcSite, OnAbcClearanceSite, OnHawthorneSite, OnHawthorneClearanceSite
                INTO #tmp_mappings
                FROM #tmp_products tp
                JOIN {nopDbName}.dbo.Product p on p.Sku = tp.Sku
                ORDER BY Id

                -- Add mappings based on store
                IF @abcStoreId IS NOT NULL
                BEGIN
	                INSERT INTO {nopDbName}.dbo.StoreMapping (EntityId, EntityName, StoreId)
	                SELECT Id, 'Product', @abcStoreId
	                FROM #tmp_mappings tm
	                WHERE tm.OnAbcSite = 1
                END
                IF @abcClearanceStoreId IS NOT NULL
                BEGIN
	                INSERT INTO {nopDbName}.dbo.StoreMapping (EntityId, EntityName, StoreId)
	                SELECT Id, 'Product', @abcClearanceStoreId
	                FROM #tmp_mappings tm
	                WHERE tm.OnAbcClearanceSite = 1
                END
                IF @hawthorneStoreId IS NOT NULL
                BEGIN
	                INSERT INTO {nopDbName}.dbo.StoreMapping (EntityId, EntityName, StoreId)
	                SELECT Id, 'Product', @hawthorneStoreId
	                FROM #tmp_mappings tm
	                WHERE tm.OnHawthorneSite = 1
                END
                IF @hawthorneClearanceStoreId IS NOT NULL
                BEGIN
	                INSERT INTO {nopDbName}.dbo.StoreMapping (EntityId, EntityName, StoreId)
	                SELECT Id, 'Product', @hawthorneClearanceStoreId
	                FROM #tmp_mappings tm
	                WHERE tm.OnHawthorneClearanceSite = 1
                END

                DROP TABLE IF EXISTS #tmp_products
                DROP TABLE IF EXISTS #tmp_mappings
            ";
            await _nopDbContext.ExecuteNonQueryAsync(command);
        }

        private async Task<List<StagingProduct>> GetCleanedStagingProductsAsync()
        {
            var stagingProducts = _stagingDb.GetProducts().ToList();
            var stagingProductsNoDupeSkus = stagingProducts.GroupBy(sp => sp.Sku).Select(sp => sp.FirstOrDefault()).ToList();

            var difference = stagingProducts.Except(stagingProductsNoDupeSkus);
            if (difference.Any())
            {
                foreach (var product in difference)
                {
                    await _logger.WarningAsync($"Product (SKU: {product.Sku}) has multiple pmap records, import skipped");
                    stagingProducts = stagingProducts.Where(sp => sp.Sku != product.Sku).ToList();
                }
            }

            // Exclude mattress product items
            var mattressItemNos = _abcMattressProductService.GetMattressItemNos();
            stagingProducts = stagingProducts.Where(sp => !mattressItemNos.Contains(sp.ISAMItemNo)).ToList();

            return stagingProducts;
        }

        private Product CreateNewProduct()
        {
            return new Product
            {
                AdditionalShippingCharge = 0,
                AdminComment = null,
                AllowAddingOnlyExistingAttributeCombinations = false,
                AllowBackInStockSubscriptions = false,
                AllowCustomerReviews = false,
                AllowedQuantities = null,
                ApprovedRatingSum = 0,
                ApprovedTotalReviews = 0,
                AutomaticallyAddRequiredProducts = false,
                AvailableEndDateTimeUtc = null,
                BasepriceAmount = 0,
                BasepriceBaseAmount = 0,
                BasepriceEnabled = false,
                BasepriceBaseUnitId = 0,
                BasepriceUnitId = 0,
                CustomerEntersPrice = false,
                DisplayOrder = 0,
                DisplayStockAvailability = false,
                DisplayStockQuantity = false,
                GiftCardTypeId = 0,
                Gtin = null,
                HasSampleDownload = false,
                HasTierPrices = false,
                HasUserAgreement = false,
                IsRecurring = false,
                IsTelecommunicationsOrBroadcastingOrElectronicServices = false,
                LowStockActivityId = 0,
                ManageInventoryMethodId = 0,
                MaximumCustomerEnteredPrice = 0,
                MaxNumberOfDownloads = 0,
                MinimumCustomerEnteredPrice = 0,
                MinStockQuantity = 0,
                NotApprovedRatingSum = 0,
                NotApprovedTotalReviews = 0,
                NotifyAdminForQuantityBelow = 0,
                OrderMaximumQuantity = 999999,
                OrderMinimumQuantity = 1,
                OverriddenGiftCardAmount = null,
                RecurringCycleLength = 0,
                RecurringTotalCycles = 0,
                RecurringCyclePeriodId = 0,
                RentalPricePeriodId = 0,
                RentalPriceLength = 0,
                RequiredProductIds = null,
                RequireOtherProducts = false,
                SampleDownloadId = 0,
                ShowOnHomepage = false,
                StockQuantity = 0,
                SubjectToAcl = false,
                UnlimitedDownloads = false,
                UseMultipleWarehouses = false,
                UserAgreementText = null,
                VendorId = 0,
                VisibleIndividually = true,
                WarehouseId = 0,
                ProductType = ProductType.SimpleProduct,
                AvailableStartDateTimeUtc = null,
                MarkAsNew = false,
                MarkAsNewStartDateTimeUtc = null,
                MarkAsNewEndDateTimeUtc = null,
                AvailableForPreOrder = false,
                PreOrderAvailabilityStartDateTimeUtc = null,
                CreatedOnUtc = DateTime.UtcNow
            };
        }

        private bool DetermineDisableBuyBasedOnNOPStock(Product product, StagingProduct stagingProduct)
        {
            return product.ManageInventoryMethod == ManageInventoryMethod.ManageStock ?
                product.StockQuantity == 0 :
                stagingProduct.DisableBuying.Value;
        }

        private async Task<Manufacturer> CreateManufacturerAsync(string name)
        {
            //create a new manufacturer if none was found
            var manufacturer = new Manufacturer
            {
                Name = name.ToUpper(),
                PageSizeOptions = "3, 6, 9, 12",
                PageSize = 16,
                Published = true,
                AllowCustomersToSelectPageSize = false,
                LimitedToStores = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await _manufacturerRepository.InsertAsync(manufacturer);
            await _urlRecordService.SaveSlugAsync(
                manufacturer,
                await _urlRecordService.ValidateSeNameAsync(
                    manufacturer,
                    "",
                    manufacturer.Name,
                    true
                ),
                0);

            return manufacturer;
        }
    }
}