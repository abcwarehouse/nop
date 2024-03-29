﻿using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tax;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcMattresses.Services;
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
    public class ImportProductsTask : IScheduleTask
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

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportProductsTask)
            {
                this.Skipped();
                return;
            }

            

            var stagingDb = _importSettings.GetStagingDbConnection();

            var ExistingSkuToId = _productRepository.Table.Select(p => new { p.Sku, p.Id }).ToDictionary(p => p.Sku, p => p.Id);

            int everythingTaxCategoryId = _taxCategoryRepository.Table.Where(tc => tc.Name == "Everything").Select(tc => tc.Id).FirstOrDefault();

            //remove all product cart price mappings and home delivery mappings
            await _nopDbContext.ExecuteNonQueryAsync($"DELETE FROM ProductCartPrice");
            await _nopDbContext.ExecuteNonQueryAsync($"DELETE FROM ProductHomeDelivery");
            await _nopDbContext.ExecuteNonQueryAsync($"DELETE FROM ProductRequiresLogin");

            var stagingProducts = await GetCleanedStagingProductsAsync();

            ProductAttribute homeDeliveryAttribute = await _importUtilities.GetHomeDeliveryAttributeAsync();
            PredefinedProductAttributeValue homeDeliveryAttributeValue = await _importUtilities.GetHomeDeliveryAttributeValueAsync();
            ProductAttribute pickupAttribute = await _importUtilities.GetPickupAttributeAsync();
            var productAttributeMappingManager = new EntityManager<ProductAttributeMapping>(EngineContext.Current.Resolve<IRepository<ProductAttributeMapping>>());
            var productCartPriceManager = new EntityManager<ProductCartPrice>(_productCartPriceRepository);
            var homeDeliveryManager = new EntityManager<ProductHomeDelivery>();
            var requiresLoginManager = new EntityManager<ProductRequiresLogin>();


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

            //creating discounts are associated to either site
            var skuToPrDiscount = new Dictionary<string, decimal>();
            var validDiscounts = _prFileDiscountService.GetPrFileDiscounts();
            if (abcWarehouseStore != null)
            {
                skuToPrDiscount = validDiscounts.Where(prd => prd.IsAbcDiscount).OrderBy(prd => prd.ProductSku).ToDictionary(prd => prd.ProductSku, prd => prd.DiscountAmount);
            }
            else if (hawthorneStore != null)
            {
                skuToPrDiscount = validDiscounts.Where(prd => prd.IsHawthorneDiscount).OrderBy(prd => prd.ProductSku).ToDictionary(prd => prd.ProductSku, prd => prd.DiscountAmount);
            }

            // preloading information for later lookups
            var productsWithPlaceholderDescriptions = new HashSet<int>(_productRepository.Table.Where(p => p.FullDescription.Contains("placeholder-features")).Select(p => p.Id).ToArray());

            var productsWithPickupAttribute = new HashSet<int>(_productAttributeMappingRepository.Table
                .Where(pam => pam.ProductAttributeId == pickupAttribute.Id).Select(pam => pam.ProductId).Distinct().ToArray());

            var productsWithHomeDeliveryAttribute = new HashSet<int>(_productAttributeMappingRepository.Table
                .Where(pam => pam.ProductAttributeId == homeDeliveryAttribute.Id).Select(pam => pam.ProductId).Distinct().ToArray());

            foreach (var stagingProduct in stagingProducts)
            {
                if (string.IsNullOrEmpty(stagingProduct.Sku))
                    continue;

                Product product = null;
                Product productSnapshot = null;
                if (ExistingSkuToId.ContainsKey(stagingProduct.Sku))
                {
                    var nopId = ExistingSkuToId[stagingProduct.Sku];
                    product = _productRepository.Table.Where(prod => prod.Id == nopId).First();
                }

                if (product != null && product.Deleted)
                {
                    continue;
                }

                var newProduct = product == null;
                var hasPlaceholderFullDescription = false;
                if (newProduct)
                {
                    product = CreateNewProduct();
                    product.TaxCategoryId = everythingTaxCategoryId;
                    newProduct = true;
                }
                else
                {
                    if (!string.IsNullOrEmpty(product.FullDescription))
                        hasPlaceholderFullDescription = productsWithPlaceholderDescriptions.Contains(product.Id);

                    productSnapshot = _importUtilities.CoreClone(product);
                }
                product.DisableBuyButton = DetermineDisableBuyBasedOnNOPStock(product, stagingProduct);
                product.Height = (stagingProduct.Height <= 0) ? 1 : stagingProduct.Height;
                product.Length = (stagingProduct.Length <= 0) ? 1 : stagingProduct.Length;
                product.ManufacturerPartNumber = stagingProduct.ManufacturerNumber;

                var hasNewName = product.Name != stagingProduct.Name;
                if (hasNewName)
                {
                    product.Name = stagingProduct.Name;
                }

                product.OldPrice = stagingProduct.BasePrice.Value > stagingProduct.DisplayPrice.Value ? stagingProduct.BasePrice.Value : stagingProduct.DisplayPrice.Value;

                if (!(product.Price != 0 && stagingProduct.SiteOnTimeSku != null && stagingProduct.BasePrice == 0))
                {
                    //apply discount to display price as needed
                    if (skuToPrDiscount.ContainsKey(stagingProduct.Sku))
                    {
                        product.Price = stagingProduct.DisplayPrice.Value - skuToPrDiscount[stagingProduct.Sku];
                    }
                    else
                    {
                        product.Price = stagingProduct.DisplayPrice.Value;
                    }
                }


                //descriptions are only overwritten if there is new content to write
                if (!string.IsNullOrEmpty(stagingProduct.ShortDescription) && (hasPlaceholderFullDescription || newProduct || string.IsNullOrEmpty(product.ShortDescription)))
                {
                    var decodedShortDescription = WebUtility.HtmlDecode(stagingProduct.ShortDescription);
                    product.ShortDescription = string.IsNullOrEmpty(decodedShortDescription) ? null : decodedShortDescription;
                }

                if (!string.IsNullOrEmpty(stagingProduct.FullDescription) && (hasPlaceholderFullDescription || newProduct || string.IsNullOrEmpty(product.FullDescription)))
                {
                    var decodedFullDescription = WebUtility.HtmlDecode(stagingProduct.FullDescription);
                    product.FullDescription = string.IsNullOrEmpty(decodedFullDescription) ? null : decodedFullDescription;
                }

                product.Sku = stagingProduct.Sku;
                product.UpdatedOnUtc = DateTime.UtcNow;
                product.Weight = (stagingProduct.Weight <= 0) ? 1 : stagingProduct.Weight;
                product.Width = (stagingProduct.Width <= 0) ? 1 : stagingProduct.Width;

                //publish the product based on stores
                product.LimitedToStores = true;
                product.Published = false;

                List<int> storeIds = new List<int>();

                // check for each different store & publish state
                if (stagingProduct.OnAbcSite
                    && abcWarehouseStore != null)
                {
                    product.Published = true;
                    storeIds.Add(abcWarehouseStore.Id);
                }

                if (stagingProduct.OnAbcClearanceSite
                    && abcClearanceStore != null)
                {
                    product.Published = true;
                    storeIds.Add(abcClearanceStore.Id);
                }

                if (stagingProduct.OnHawthorneSite
                    && hawthorneStore != null)
                {
                    product.Published = true;
                    storeIds.Add(hawthorneStore.Id);
                }

                if (stagingProduct.OnHawthorneClearanceSite.HasValue
                    && stagingProduct.OnHawthorneClearanceSite.Value
                    && hawthorneClearanceStore != null)
                {
                    product.Published = true;
                    storeIds.Add(hawthorneClearanceStore.Id);
                }

                product.IsShipEnabled = true;

                if (product.Price <= 0 && !await product.IsCallOnlyAsync())
                {
                    product.Published = false;
                }

                if (await product.IsCallOnlyAsync())
                {
                    product.CallForPrice = true;
                    product.DisableBuyButton = true;
                }

                //update or insert as needed
                if (newProduct)
                {
                    //have to insert directly to repo to get back the id
                    await _productRepository.InsertAsync(product);
                    ExistingSkuToId[product.Sku] = product.Id;
                    await _urlRecordService.SaveSlugAsync(product, await _urlRecordService.ValidateSeNameAsync(product, "", product.Name, true), 0);

                    if (stagingProduct.AllowInStorePickup)
                    {
                        await _importUtilities.InsertProductAttributeMappingAsync(
                            product.Id,
                            pickupAttribute.Id,
                            productAttributeMappingManager
                        );
                    }

                    if (!stagingProduct.CanUseUps)
                    {
                        await _importUtilities.InsertProductAttributeMappingAsync(
                            product.Id,
                            homeDeliveryAttribute.Id,
                            productAttributeMappingManager
                        );
                    }
                }
                else
                {
                    //only update if the product has been changed
                    if (!_importUtilities.CoreEquals(productSnapshot, product))
                    {
                        product.Deleted = false;
                        await _productService.UpdateProductAsync(product);
                    }

                    // straight sql update
                    //update url record if name has changed
                    if (hasNewName)
                    {
                        var slug = await _urlRecordService.ValidateSeNameAsync(product, "", product.Name, true);
                        await _urlRecordService.SaveSlugAsync<Product>(
                            product,
                            slug,
                            0
                        );
                    }

                    if (!productsWithPickupAttribute.Contains(product.Id) && stagingProduct.AllowInStorePickup)
                    {
                        // a mapping to the pickup attribute does not exist and is needed, add one
                        await _importUtilities.InsertProductAttributeMappingAsync(
                            product.Id,
                            pickupAttribute.Id,
                            productAttributeMappingManager
                        );
                    }
                    else if (productsWithPickupAttribute.Contains(product.Id) && !stagingProduct.AllowInStorePickup)
                    {
                        // a mapping to the pickup attribute exists and is not needed, remove it
                        ProductAttributeMapping pickupAttributeMapping =
                            (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id))
                        .Where(pam => pam.ProductAttributeId == pickupAttribute.Id)
                        .Select(pam => pam).FirstOrDefault();
                        // updated to not allow pick up in store anymore
                        await _productAttributeService.DeleteProductAttributeMappingAsync(pickupAttributeMapping);
                    }


                    if (!productsWithHomeDeliveryAttribute.Contains(product.Id) && !stagingProduct.CanUseUps)
                    {
                        await _importUtilities.InsertProductAttributeMappingAsync(
                            product.Id,
                            homeDeliveryAttribute.Id,
                            productAttributeMappingManager
                        );
                    }
                    else if (productsWithHomeDeliveryAttribute.Contains(product.Id) && stagingProduct.CanUseUps)
                    {
                        ProductAttributeMapping homeDeliveryAttributeMapping =
                            (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id))
                        .Where(pam => pam.ProductAttributeId == homeDeliveryAttribute.Id)
                        .Select(pam => pam).FirstOrDefault();
                        // updated to not allow home delivery anymore
                        await _productAttributeService.DeleteProductAttributeMappingAsync(homeDeliveryAttributeMapping);
                    }
                }

                //add manufacturer and mappings as needed
                if (stagingProduct.Manufacturer != null)
                {
                    var manufacturers = await _manufacturerService.GetManufacturersByNameAsync(stagingProduct.Manufacturer);
                    var manufacturer = manufacturers.FirstOrDefault() ?? await CreateManufacturerAsync(stagingProduct.Manufacturer);
                    if (manufacturer != null)
                    {
                        var productManufacturers = _productManufacturerRepository.Table
                            .Where(pm => pm.ManufacturerId == manufacturer.Id && pm.ProductId == product.Id);
                        if (!productManufacturers.Any())
                        {
                            await _manufacturerService.InsertProductManufacturerAsync(
                                new ProductManufacturer { ProductId = product.Id, ManufacturerId = manufacturer.Id }
                            );
                        }
                    }
                }

                //add to cart price table if needed
                Staging.PriceBucketCode priceBucketCode = (Staging.PriceBucketCode)stagingProduct.PriceBucketCode;

                if (priceBucketCode == Staging.PriceBucketCode.AddToCartForCurrentPricing ||
                    priceBucketCode == Staging.PriceBucketCode.AddToCartForCurrentPricingRequireLogin)
                {
                    await productCartPriceManager.InsertAsync(new ProductCartPrice { Product_Id = product.Id, CartPrice = stagingProduct.CartPrice.Value });
                    if (priceBucketCode == Staging.PriceBucketCode.AddToCartForCurrentPricingRequireLogin)
                    {
                        await requiresLoginManager.InsertAsync(new ProductRequiresLogin { Product_Id = product.Id });
                    }
                }

                //add to home delivery table if needed
                if (!stagingProduct.CanUseUps)
                {
                    await homeDeliveryManager.InsertAsync(new ProductHomeDelivery { Product_Id = product.Id });
                }

                // After collecting all the store IDs to which this product relates,
                // distinct it and run over each of them to set the store mappings.
                storeIds = storeIds.Distinct().ToList();
                foreach (var store in storeIds)
                {
                    // For any manufacturer for the product, set it's mapping.
                    // Should put this into a service (override the capability that pulls by store)
                    var productManufacturers = _productManufacturerRepository.Table.Where(pm => pm.ProductId == product.Id);

                    foreach (var prodManMappings in productManufacturers)
                    {
                        var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(prodManMappings.ManufacturerId);

                        // If the manufacturer is already mapped to this store, skip it.
                        if ((await _storeMappingService
                            .GetStoresIdsWithAccessAsync(manufacturer))
                            .Contains(store))
                        {
                            continue;
                        }
                        await _storeMappingService.InsertStoreMappingAsync(
                            manufacturer, store);
                    }
                }

                await UpdateAddToCartInfoAsync(priceBucketCode, product, stagingProduct);
                await SetFullDescriptionIfEmptyAsync(stagingProduct, product);

                if (!string.IsNullOrWhiteSpace(stagingProduct.Upc))
                {
                    product.Gtin = stagingProduct.Upc;
                    await _productService.UpdateProductAsync(product);
                }

                // get package product
                var sku = _frontendService.GetProductIdInPackage(product.Sku)?.Sku ?? product.Sku;
                await _genericAttributeService.SaveAttributeAsync(product, "PowerReviewsSku", sku);
            }

            await ImportProductStoreMappingsAsync();

            await productAttributeMappingManager.FlushAsync();
            await productCartPriceManager.FlushAsync();
            await homeDeliveryManager.FlushAsync();
            await requiresLoginManager.FlushAsync();

            // marking products that do not exist in the staging database as deleted
            var nopDbName = DataSettingsManager.LoadSettings().ConnectionString.GetDatabaseName();
            var stagingDbName = stagingDb.Database;
            var mattressItemAddition = _importSettings.SkipOldMattressesImport ?
                " AND Product.Id NOT IN (SELECT ProductId FROM [AbcMattressModel])" :
                "";
            var deleteCommand = $@"
                update[{nopDbName}].dbo.Product set Published = 0, Deleted = 1
                from [{nopDbName}].[dbo].[Product] 
                left join[{stagingDbName}].[dbo].[ProductSource] as ps on ([{nopDbName}].[dbo].[Product].[Sku] = ps.IsamSku
                                                                   OR[{nopDbName}].[dbo].[Product].[Sku] = ps.SiteOnTimeSku)
                where ps.IsamSku is null AND ps.SiteOnTimeSku is null
                {mattressItemAddition}
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

            await _nopDbContext.ExecuteNonQueryAsync("EXECUTE [dbo].[SanitizeSOTShortDescriptions];");

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
            var mattressItemAddition = _importSettings.SkipOldMattressesImport ?
                " AND EntityId NOT IN (SELECT ProductId FROM [AbcMattressModel])" :
                "";
            var command = $@"
                DROP TABLE IF EXISTS #tmp_products
                DROP TABLE IF EXISTS #tmp_mappings

                -- Clear all current product mappings
                DELETE FROM {nopDbName}.dbo.StoreMapping
                WHERE EntityName = 'Product'
                {mattressItemAddition}

                -- Gets a full list of ABC and SOT items from staging DB
                SELECT DISTINCT
	                ISNULL(p.Id, SotP.Id) as Id,
	                ISNULL(p.Name,SotP.Name)as Name,
	                CAST(CASE
		                WHEN SotP.ShortDescription is not null
			                THEN SotP.ShortDescription
		                ELSE p.ShortDescription
	                END AS NVARCHAR(MAX)) as ShortDescription,
	                CAST(CASE
		                WHEN SotP.FullDescription is not null
			                THEN SotP.FullDescription
		                ELSE NULL
	                END AS NVARCHAR(MAX)) as FullDescription,
	                ISNULL(p.OnAbcSite,SotP.OnAbcSite) as OnAbcSite,
	                ISNULL(p.OnHawthorneSite,SotP.OnHawthorneSite) as OnHawthorneSite,
	                ISNULL(p.OnAbcClearanceSite,SotP.OnAbcClearanceSite) as OnAbcClearanceSite,
	                ISNULL(p.OnHawthorneClearanceSite,SotP.OnHawthorneClearanceSite) as OnHawthorneClearanceSite,
	                ISNULL(p.Sku,SotP.Sku) as Sku,
	                ISNULL(p.ManufacturerNumber,SotP.ManufacturerNumber) as ManufacturerNumber,
	                ISNULL(m.Name, SotP.Manufacturer) as Manufacturer,
	                ISNULL(p.DisableBuying,SotP.DisableBuying) as DisableBuying,
	                ISNULL(p.[Weight],SotP.[Weight]) as [Weight],
	                ISNULL(p.[Length],SotP.[Length]) as [Length],
	                ISNULL(p.Width,SotP.Width) as Width,
	                ISNULL(p.Height,SotP.Height) as Height,
	                ISNULL(p.AllowInStorePickup,SotP.AllowInStorePickup) as AllowInStorePickup,
	                ISNULL(p.IsNew,SotP.IsNew) as IsNew,
	                ISNULL(p.NewExpectedDate,SotP.NewExpectedDate) as NewExpectedDate,
	                ISNULL(p.LimitedStockDate,SotP.LimitedStockDate) as LimitedStockDate,
	                ISNULL(p.BasePrice,SotP.BasePrice) as BasePrice,
                    ISNULL(p.DisplayPrice,SotP.DisplayPrice) as [DisplayPrice],
                    ISNULL(p.CartPrice,SotP.CartPrice) as [CartPrice],
                    ISNULL(p.UsePairPricing,SotP.UsePairPricing) as [UsePairPricing],
                    ISNULL(p.PriceBucketCode,SotP.PriceBucketCode) as [PriceBucketCode],
	                ISNULL(p.CanUseUps, 0) as [CanUseUps], -- All SOT products are in Home Delivery
	                p.ItemNumber as [ISAMItemNo],
	                SotP.Sku as [SiteOnTimeSku],
	                ISNULL(SotP.Upc,p.Upc) as [Upc],
	                p.FactTag as [FactTag]
	                INTO #tmp_products
	                FROM {stagingDbName}.dbo.[Product] as p
	                LEFT OUTER JOIN {stagingDbName}.dbo.ProductManufacturerMapping ON Sku = ItemSku
	                LEFT OUTER JOIN {stagingDbName}.dbo.Manufacturer as m ON ProductManufacturerMapping.BrandCode = m.BrandCode
	                FULL OUTER JOIN {stagingDbName}.[dbo].[SiteOnTimeProduct] as SotP ON p.Sku = SotP.Sku

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

            if (_importSettings.SkipOldMattressesImport)
            {
                var mattressItemNos = _abcMattressProductService.GetMattressItemNos();
                stagingProducts = stagingProducts.Where(sp => !mattressItemNos.Contains(sp.ISAMItemNo)).ToList();
            }

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