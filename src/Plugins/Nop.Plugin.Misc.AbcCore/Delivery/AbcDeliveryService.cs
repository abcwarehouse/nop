using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Data;
using Nop.Services.Logging;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcCore.Delivery
{
    public class AbcDeliveryService : IAbcDeliveryService
    {
        private readonly ICategoryService _categoryService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeService _productAttributeService;
        private readonly ILogger _logger;
        private readonly IRepository<AbcDeliveryItem> _abcDeliveryItemRepository;
        private readonly IRepository<AbcDeliveryMap> _abcDeliveryMapRepository;

        public AbcDeliveryService(
            ICategoryService categoryService,
            IPriceFormatter priceFormatter,
            IProductAttributeService productAttributeService,
            ILogger logger,
            IRepository<AbcDeliveryItem> abcDeliveryItemRepository,
            IRepository<AbcDeliveryMap> abcDeliveryMapRepository)
        {
            _categoryService = categoryService;
            _priceFormatter = priceFormatter;
            _productAttributeService = productAttributeService;
            _logger = logger;
            _abcDeliveryItemRepository = abcDeliveryItemRepository;
            _abcDeliveryMapRepository = abcDeliveryMapRepository;
        }

        private async System.Threading.Tasks.Task<(int deliveryPickupOptionsProductAttributeId,
                                                   int haulAwayDeliveryProductAttributeId,
                                                   int haulAwayDeliveryInstallProductAttributeId)> GetAbcDeliveryProductAttributesAsync()
        {
            var productAttributes = await _productAttributeService.GetAllProductAttributesAsync();

            int _deliveryPickupOptionsProductAttributeId = productAttributes
                .Where(p => p.Name == AbcDeliveryConsts.DeliveryPickupOptions)
                .Select(p => p.Id)
                .Single();

            int _haulAwayDeliveryProductAttributeId = productAttributes
                .Where(p => p.Name == AbcDeliveryConsts.HaulAwayDelivery)
                .Select(p => p.Id)
                .Single();

            int _haulAwayDeliveryInstallProductAttributeId = productAttributes
                .Where(p => p.Name == AbcDeliveryConsts.HaulAwayDeliveryInstall)
                .Select(p => p.Id)
                .Single();

            return (
                _deliveryPickupOptionsProductAttributeId,
                _haulAwayDeliveryProductAttributeId,
                _haulAwayDeliveryInstallProductAttributeId
            );
        }

        public async Task<AbcDeliveryItem> GetAbcDeliveryItemByItemNumberAsync(int itemNumber)
        {
            try
            {
                return await _abcDeliveryItemRepository.Table.SingleAsync(adi => adi.Item_Number == itemNumber);
            }
            catch (Exception)
            {
                await _logger.ErrorAsync($"Cannot find single AbcDeliveryItem with ItemNumber {itemNumber}");
                throw;
            }
        }

        // Will only return options with mapped delivery options
        // May not need to be made public
        public async Task<AbcDeliveryMap> GetAbcDeliveryMapByCategoryIdAsync(int categoryId)
        {
            var adm = await _abcDeliveryMapRepository.Table.FirstOrDefaultAsync(adm => adm.CategoryId == categoryId);

            return adm != null && adm.HasDeliveryOptions() ? adm : null;
        }

        public async Task UpdateProductDeliveryOptionsAsync(
            Product product,
            bool allowInStorePickup)
        {
            var categoryId = (await _categoryService.GetProductCategoriesByProductIdAsync(product.Id))
                .Select(pc => pc.CategoryId).FirstOrDefault();
            var abcDeliveryMap = await GetAbcDeliveryMapByCategoryIdAsync(categoryId);
            if (abcDeliveryMap == null)
            {
                return;
            }

            (int deliveryOptionsPamId,
             int? deliveryPavId,
             int? deliveryInstallPavId,
             decimal? deliveryPriceAdjustment,
             decimal? deliveryInstallPriceAdjustment) = await AddDeliveryOptionsAsync(
                 product.Id,
                 abcDeliveryMap,
                 allowInStorePickup);

             await AddHaulAwayAsync(
                product.Id,
                abcDeliveryMap,
                deliveryOptionsPamId,
                deliveryPavId,
                deliveryInstallPavId,
                deliveryPriceAdjustment.HasValue ? deliveryPriceAdjustment.Value : 0M,
                deliveryInstallPriceAdjustment.HasValue ? deliveryInstallPriceAdjustment.Value : 0M);
        }

        private async System.Threading.Tasks.Task AddHaulAwayAsync(
            int productId,
            AbcDeliveryMap map,
            int deliveryOptionsPamId,
            int? deliveryPavId,
            int? deliveryInstallPavId,
            decimal deliveryPriceAdjustment,
            decimal deliveryInstallPriceAdjustment)
        {
            // Haulaway (Delivery)
            if (deliveryPavId.HasValue)
            {
                await AddHaulAwayAttributeAsync(
                    productId,
                    (await GetAbcDeliveryProductAttributesAsync()).haulAwayDeliveryProductAttributeId,
                    deliveryOptionsPamId,
                    deliveryPavId.Value,
                    map.DeliveryHaulway,
                    deliveryPriceAdjustment);
            }

            // Haulaway (Delivery/Install)
            if (deliveryInstallPavId.HasValue)
            {
                await AddHaulAwayAttributeAsync(
                    productId,
                    (await GetAbcDeliveryProductAttributesAsync()).haulAwayDeliveryInstallProductAttributeId,
                    deliveryOptionsPamId,
                    deliveryInstallPavId.Value,
                    map.DeliveryHaulwayInstall,
                    deliveryInstallPriceAdjustment);
            }
        }

        private async System.Threading.Tasks.Task<ProductAttributeMapping> AddHaulAwayAttributeMappingAsync(
            int productId,
            int productAttributeId,
            int deliveryOptionsPamId,
            int? pavId)
        {
            if (pavId == null)
            {
                return null;
            }

            var pam = (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId))
                                                    .SingleOrDefault(pam => pam.ProductAttributeId == productAttributeId);
            if (pam == null)
            {
                pam = new ProductAttributeMapping()
                {
                    ProductId = productId,
                    ProductAttributeId = productAttributeId,
                    AttributeControlType = AttributeControlType.Checkboxes,
                    TextPrompt = "Haul Away",
                    ConditionAttributeXml = $"<Attributes><ProductAttribute ID=\"{deliveryOptionsPamId}\"><ProductAttributeValue><Value>{pavId}</Value></ProductAttributeValue></ProductAttribute></Attributes>",
                };
                await _productAttributeService.InsertProductAttributeMappingAsync(pam);
            }

            return pam;
        }

        private async System.Threading.Tasks.Task AddHaulAwayAttributeAsync(
            int productId,
            int productAttributeId,
            int deliveryOptionsPamId,
            int deliveryOptionsPavId,
            int abcDeliveryMapItemNumber,
            decimal priceAdjustment)
        {
            var pam = await AddHaulAwayAttributeMappingAsync(
                productId,
                (await GetAbcDeliveryProductAttributesAsync()).haulAwayDeliveryProductAttributeId,
                deliveryOptionsPamId,
                deliveryOptionsPavId);
            if (pam == null)
            {
                return;
            }

            var pav = (await _productAttributeService.GetProductAttributeValuesAsync(pam.Id)).FirstOrDefault();
            await AddValueAsync(
                pam.Id,
                pav,
                abcDeliveryMapItemNumber,
                "Remove Old Appliance ({0})",
                0,
                false,
                priceAdjustment);
        }

        private async System.Threading.Tasks.Task<(int pamId,
                                                   int? deliveryPavId,
                                                   int? deliveryInstallPavId,
                                                   decimal? deliveryPriceAdjustment,
                                                   decimal? deliveryInstallPriceAdjustment)> AddDeliveryOptionsAsync(
            int productId,
            AbcDeliveryMap map,
            bool allowInStorePickup)
        {
            var pam = await AddDeliveryOptionsAttributeAsync(productId);
            var values = await _productAttributeService.GetProductAttributeValuesAsync(pam.Id);

            var deliveryOnlyPav = values.Where(pav => pav.Name.Contains("Home Delivery (")).SingleOrDefault();
            deliveryOnlyPav = await AddValueAsync(
                pam.Id,
                deliveryOnlyPav,
                map.DeliveryOnly,
                "Home Delivery ({0}, FREE With Mail-In Rebate)",
                10,
                true);

            var deliveryInstallationPav = values.Where(pav => pav.Name.Contains("Home Delivery and Installation (")).SingleOrDefault();
            deliveryInstallationPav = await AddValueAsync(
                pam.Id,
                deliveryInstallationPav,
                map.DeliveryInstall,
                "Home Delivery and Installation ({0})",
                20,
                deliveryOnlyPav == null);

            if (allowInStorePickup)
            {
                var pickupPav = values.Where(pav => pav.Name.Contains("Pickup In-Store")).SingleOrDefault();
                if (pickupPav == null)
                {
                    var newPickupPav = new ProductAttributeValue()
                    {
                        ProductAttributeMappingId = pam.Id,
                        Name = "Pickup In-Store Or Curbside (FREE)",
                        DisplayOrder = 0,
                    };
                    await _productAttributeService.InsertProductAttributeValueAsync(newPickupPav);
                }
            }

            return (pam.Id, deliveryOnlyPav?.Id, deliveryInstallationPav?.Id, deliveryOnlyPav?.PriceAdjustment, deliveryInstallationPav?.PriceAdjustment);
        }

        private async System.Threading.Tasks.Task<ProductAttributeMapping> AddDeliveryOptionsAttributeAsync(int productId)
        {
            var paId = (await GetAbcDeliveryProductAttributesAsync()).deliveryPickupOptionsProductAttributeId;
            var pam = (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId))
                                                     .SingleOrDefault(pam => pam.ProductAttributeId == paId);
            if (pam == null)
            {
                pam = new ProductAttributeMapping()
                {
                    ProductId = productId,
                    ProductAttributeId = (await GetAbcDeliveryProductAttributesAsync()).deliveryPickupOptionsProductAttributeId,
                    AttributeControlType = AttributeControlType.RadioList,
                };
                await _productAttributeService.InsertProductAttributeMappingAsync(pam);
            }

            return pam;
        }

        private async System.Threading.Tasks.Task<ProductAttributeValue> AddValueAsync(
            int pamId,
            ProductAttributeValue pav,
            int itemNumber,
            string displayName,
            int displayOrder,
            bool isPreSelected,
            decimal priceAdjustment = 0)
        {
            if (pav == null && itemNumber != 0)
            {
                var item = await GetAbcDeliveryItemByItemNumberAsync(itemNumber);
                var price = item.Price - priceAdjustment;
                var priceDisplay = price == 0 ?
                    "FREE" :
                    await _priceFormatter.FormatPriceAsync(price);
                pav = new ProductAttributeValue()
                {
                    ProductAttributeMappingId = pamId,
                    Name = string.Format(displayName, priceDisplay),
                    Cost = itemNumber,
                    PriceAdjustment = price,
                    IsPreSelected = isPreSelected,
                    DisplayOrder = displayOrder,
                };

                await _productAttributeService.InsertProductAttributeValueAsync(pav);
            }

            return pav;
        }
    }
}
