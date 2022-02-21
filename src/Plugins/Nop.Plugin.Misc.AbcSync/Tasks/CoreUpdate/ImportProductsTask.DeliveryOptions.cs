using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Delivery;
using Nop.Services.Tasks;

namespace Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate
{
    public partial class ImportProductsTask : IScheduleTask
    {
        private async System.Threading.Tasks.Task InitializeDeliveryOptionsProductAttributesAsync()
        {
            _deliveryPickupOptionsProductAttribute = await SaveProductAttributeAsync(AbcDeliveryConsts.DeliveryPickupOptionsProductAttributeName);
            _haulAwayDeliveryProductAttribute = await SaveProductAttributeAsync(AbcDeliveryConsts.HaulAwayDeliveryProductAttributeName);
            _haulAwayDeliveryInstallProductAttribute = await SaveProductAttributeAsync(AbcDeliveryConsts.HaulAwayDeliveryInstallProductAttributeName);
        }

        private async System.Threading.Tasks.Task<ProductAttribute> SaveProductAttributeAsync(string name)
        {
            var attribute = new ProductAttribute()
            {
                Name = name
            };
            await _productAttributeService.SaveProductAttributeAsync(attribute);

            return attribute;
        }

        private async System.Threading.Tasks.Task<IList<ProductAttributeMapping>> UpdateDeliveryProductAttributeMappingsAsync(int productId)
        {
            var pams = new List<ProductAttributeMapping>();
            var categoryId = (await _categoryService.GetProductCategoriesByProductIdAsync(productId)).Select(pc => pc.CategoryId).FirstOrDefault();
            var abcDeliveryMap = await _abcDeliveryService.GetAbcDeliveryMapByCategoryIdAsync(categoryId);
            if (abcDeliveryMap != null)
            {
                if (abcDeliveryMap.DeliveryOnly != 0 || abcDeliveryMap.DeliveryInstall != 0)
                {
                    pams.Add(new ProductAttributeMapping()
                    {
                        ProductId = productId,
                        ProductAttributeId = _deliveryPickupOptionsProductAttribute.Id,
                        AttributeControlType = AttributeControlType.RadioList,
                    });
                }

                var deliveryOptionsPam = (await _productAttributeService.SaveProductAttributeMappingsAsync(productId, pams, new string[]{
                    AbcDeliveryConsts.HaulAwayDeliveryProductAttributeName,
                    AbcDeliveryConsts.HaulAwayDeliveryInstallProductAttributeName
                })).SingleOrDefault();

                if (abcDeliveryMap.DeliveryHaulway != 0)
                {
                    pams.Add(new ProductAttributeMapping()
                    {
                        ProductId = productId,
                        ProductAttributeId = _haulAwayDeliveryProductAttribute.Id,
                        AttributeControlType = AttributeControlType.Checkboxes,
                        DisplayOrder = 10,
                        TextPrompt = AbcDeliveryConsts.HaulAwayTextPrompt
                        // Needs condition - need ID from delivery options
                    });
                }
                if (abcDeliveryMap.DeliveryHaulwayInstall != 0)
                {
                    pams.Add(new ProductAttributeMapping()
                    {
                        ProductId = productId,
                        ProductAttributeId = _haulAwayDeliveryInstallProductAttribute.Id,
                        AttributeControlType = AttributeControlType.Checkboxes,
                        DisplayOrder = 20,
                        TextPrompt = AbcDeliveryConsts.HaulAwayTextPrompt
                        // Needs condition - need ID from delivery options
                    });
                }

                // Need to remove the "Delivery Options" pam so no duplicate below.
                pams.RemoveAll(pam => pam.Id == deliveryOptionsPam?.Id);
            }

            // Need to save now without Delivery Options
            await _productAttributeService.SaveProductAttributeMappingsAsync(productId, pams, new string[]{
                AbcDeliveryConsts.DeliveryPickupOptionsProductAttributeName
            });

            return pams;
        }

        // private async Task UpdateProductDeliveryOptionsAsync(
        //     Product product,
        //     bool allowInStorePickup)
        // {
        //     var categoryId = (await _categoryService.GetProductCategoriesByProductIdAsync(product.Id))
        //         .Select(pc => pc.CategoryId).FirstOrDefault();
        //     var abcDeliveryMap = await GetAbcDeliveryMapByCategoryIdAsync(categoryId);
        //     if (abcDeliveryMap == null)
        //     {
        //         return;
        //     }

        //     (int deliveryOptionsPamId,
        //      int? deliveryPavId,
        //      int? deliveryInstallPavId,
        //      decimal? deliveryPriceAdjustment,
        //      decimal? deliveryInstallPriceAdjustment) = await AddDeliveryOptionsAsync(
        //          product.Id,
        //          abcDeliveryMap,
        //          allowInStorePickup);

        //     //  await AddHaulAwayAsync(
        //     //     product.Id,
        //     //     abcDeliveryMap,
        //     //     deliveryOptionsPamId,
        //     //     deliveryPavId,
        //     //     deliveryInstallPavId,
        //     //     deliveryPriceAdjustment.HasValue ? deliveryPriceAdjustment.Value : 0M,
        //     //     deliveryInstallPriceAdjustment.HasValue ? deliveryInstallPriceAdjustment.Value : 0M);
        // }

        // private async System.Threading.Tasks.Task<(int pamId,
        //                                            int? deliveryPavId,
        //                                            int? deliveryInstallPavId,
        //                                            decimal? deliveryPriceAdjustment,
        //                                            decimal? deliveryInstallPriceAdjustment)> AddDeliveryOptionsAsync(
        //     int productId,
        //     AbcDeliveryMap map,
        //     bool allowInStorePickup)
        // {
        //     var pam = await AddDeliveryOptionsAttributeAsync(productId);
        //     var values = await _productAttributeService.GetProductAttributeValuesAsync(pam.Id);

        //     var deliveryOnlyPav = values.Where(pav => pav.Name.Contains("Home Delivery (")).SingleOrDefault();
        //     deliveryOnlyPav = await AddValueAsync(
        //         pam.Id,
        //         deliveryOnlyPav,
        //         map.DeliveryOnly,
        //         "Home Delivery ({0}, FREE With Mail-In Rebate)",
        //         10,
        //         true);

        //     var deliveryInstallationPav = values.Where(pav => pav.Name.Contains("Home Delivery and Installation (")).SingleOrDefault();
        //     deliveryInstallationPav = await AddValueAsync(
        //         pam.Id,
        //         deliveryInstallationPav,
        //         map.DeliveryInstall,
        //         "Home Delivery and Installation ({0})",
        //         20,
        //         deliveryOnlyPav == null);

        //     if (allowInStorePickup)
        //     {
        //         var pickupPav = values.Where(pav => pav.Name.Contains("Pickup In-Store")).SingleOrDefault();
        //         if (pickupPav == null)
        //         {
        //             var newPickupPav = new ProductAttributeValue()
        //             {
        //                 ProductAttributeMappingId = pam.Id,
        //                 Name = "Pickup In-Store Or Curbside (FREE)",
        //                 DisplayOrder = 0,
        //             };
        //             await _productAttributeService.InsertProductAttributeValueAsync(newPickupPav);
        //         }
        //     }

        //     return (pam.Id, deliveryOnlyPav?.Id, deliveryInstallationPav?.Id, deliveryOnlyPav?.PriceAdjustment, deliveryInstallationPav?.PriceAdjustment);
        // }

        // private async System.Threading.Tasks.Task<ProductAttributeMapping> AddDeliveryOptionsAttributeAsync(int productId)
        // {
        //     var pam = (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId))
        //                                              .SingleOrDefault(pam => pam.ProductAttributeId == _deliveryPickupOptionsProductAttribute.Id);
        //     if (pam == null)
        //     {
        //         pam = new ProductAttributeMapping()
        //         {
        //             ProductId = productId,
        //             ProductAttributeId = (await GetAbcDeliveryProductAttributesAsync()).deliveryPickupOptionsProductAttributeId,
        //             AttributeControlType = AttributeControlType.RadioList,
        //         };
        //         await _productAttributeService.InsertProductAttributeMappingAsync(pam);
        //     }

        //     return pam;
        // }
    }
}