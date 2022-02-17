using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Services.Tasks;

namespace Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate
{
    public partial class ImportProductsTask : IScheduleTask
    {
        private async System.Threading.Tasks.Task ProcessStagingProductAsync(StagingProduct stagingProduct)
        {
            if (string.IsNullOrEmpty(stagingProduct.Sku))
                return;

            Product product = null;
            Product productSnapshot = null;
            if (_existingSkuToId.ContainsKey(stagingProduct.Sku))
            {
                var nopId = _existingSkuToId[stagingProduct.Sku];
                product = _productRepository.Table.Where(prod => prod.Id == nopId).First();
            }

            if (product != null)
            {
                // SOT products are being left alone completely to allow for manual intervention.
                if (product.Deleted || stagingProduct.ISAMItemNo == null)
                {
                    return;
                }
            }

            var newProduct = product == null;
            var hasPlaceholderFullDescription = false;
            if (newProduct)
            {
                product = CreateNewProduct();
                product.TaxCategoryId = _everythingTaxCategoryId;
                newProduct = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(product.FullDescription))
                    hasPlaceholderFullDescription = _productIdsWithPlaceholderDescriptions.Contains(product.Id);

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

            if (!(product.Price != 0 && stagingProduct.BasePrice == 0))
            {
                //apply discount to display price as needed
                if (_skuToPrDiscount.ContainsKey(stagingProduct.Sku))
                {
                    product.Price = stagingProduct.DisplayPrice.Value - _skuToPrDiscount[stagingProduct.Sku];
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
                    && _abcWarehouseStore != null)
                {
                    product.Published = true;
                    storeIds.Add(_abcWarehouseStore.Id);
                }

                if (stagingProduct.OnAbcClearanceSite
                    && _abcClearanceStore != null)
                {
                    product.Published = true;
                    storeIds.Add(_abcClearanceStore.Id);
                }

                if (stagingProduct.OnHawthorneSite
                    && _hawthorneStore != null)
                {
                    product.Published = true;
                    storeIds.Add(_hawthorneStore.Id);
                }

                if (stagingProduct.OnHawthorneClearanceSite.HasValue
                    && stagingProduct.OnHawthorneClearanceSite.Value
                    && _hawthorneClearanceStore != null)
                {
                    product.Published = true;
                    storeIds.Add(_hawthorneClearanceStore.Id);
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
                    _existingSkuToId[product.Sku] = product.Id;
                    await _urlRecordService.SaveSlugAsync(product, await _urlRecordService.ValidateSeNameAsync(product, "", product.Name, true), 0);

                    if (!stagingProduct.CanUseUps)
                    {
                        await _importUtilities.InsertProductAttributeMappingAsync(
                            product.Id,
                            _homeDeliveryAttribute.Id,
                            _productAttributeMappingManager
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

                    if (!_productIdsWithHomeDeliveryAttribute.Contains(product.Id) && !stagingProduct.CanUseUps)
                    {
                        await _importUtilities.InsertProductAttributeMappingAsync(
                            product.Id,
                            _homeDeliveryAttribute.Id,
                            _productAttributeMappingManager
                        );
                    }
                    else if (_productIdsWithHomeDeliveryAttribute.Contains(product.Id) && stagingProduct.CanUseUps)
                    {
                        ProductAttributeMapping homeDeliveryAttributeMapping =
                            (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id))
                        .Where(pam => pam.ProductAttributeId == _homeDeliveryAttribute.Id)
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
                    await _productCartPriceManager.InsertAsync(new ProductCartPrice { Product_Id = product.Id, CartPrice = stagingProduct.CartPrice.Value });
                    if (priceBucketCode == Staging.PriceBucketCode.AddToCartForCurrentPricingRequireLogin)
                    {
                        await _requiresLoginManager.InsertAsync(new ProductRequiresLogin { Product_Id = product.Id });
                    }
                }

                //add to home delivery table if needed
                if (!stagingProduct.CanUseUps)
                {
                    await _homeDeliveryManager.InsertAsync(new ProductHomeDelivery { Product_Id = product.Id });
                }

                // After collecting all the store IDs to which this product relates,
                // distinct it and run over each of them to set the store mappings.
                storeIds = storeIds.Distinct().ToList();
                foreach (var storeId in storeIds)
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
                            .Contains(storeId))
                        {
                            continue;
                        }
                        await _storeMappingService.InsertStoreMappingAsync(
                            manufacturer, storeId);
                    }
                }

                await UpdateAddToCartInfoAsync(priceBucketCode, product, stagingProduct);
                await SetFullDescriptionIfEmptyAsync(stagingProduct, product);

                if (!string.IsNullOrWhiteSpace(stagingProduct.Upc))
                {
                    product.Gtin = stagingProduct.Upc;
                    await _productService.UpdateProductAsync(product);
                }
        }
    }
}