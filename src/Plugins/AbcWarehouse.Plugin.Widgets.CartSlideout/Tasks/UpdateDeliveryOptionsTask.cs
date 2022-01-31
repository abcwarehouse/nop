using System.Linq;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Domain;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Services;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Tasks;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout
{
    public class UpdateDeliveryOptionsTask : IScheduleTask
    {
        private readonly IAbcDeliveryService _abcDeliveryService;
        private readonly ICategoryService _categoryService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeService _productAttributeService;

        private int _deliveryPickupOptionsProductAttributeId;

        public UpdateDeliveryOptionsTask(
            IAbcDeliveryService abcDeliveryService,
            ICategoryService categoryService,
            IPriceFormatter priceFormatter,
            IProductAttributeService productAttributeService)
        {
            _abcDeliveryService = abcDeliveryService;
            _categoryService = categoryService;
            _priceFormatter = priceFormatter;
            _productAttributeService = productAttributeService;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            _deliveryPickupOptionsProductAttributeId =
                (await _productAttributeService.GetAllProductAttributesAsync())
                .Where(p => p.Name == "Delivery/Pickup Options")
                .Select(p => p.Id)
                .Single();

            var abcDeliveryMaps = await _abcDeliveryService.GetAbcDeliveryMapsAsync();

            foreach (var abcDeliveryMap in abcDeliveryMaps)
            {
                var productIds = (await _categoryService.GetProductCategoriesByCategoryIdAsync(abcDeliveryMap.CategoryId)).Select(pc => pc.ProductId);
                foreach (var productId in productIds)
                {
                    var pam = await AddDeliveryOptionsAttributeAsync(productId);
                    await AddDeliveryOptionsValuesAsync(pam.Id, abcDeliveryMap);
                }
            }
        }

        private async System.Threading.Tasks.Task<ProductAttributeMapping> AddDeliveryOptionsAttributeAsync(int productId)
        {
            var pam = (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId))
                                                             .SingleOrDefault(pam => pam.ProductAttributeId == _deliveryPickupOptionsProductAttributeId);
            if (pam == null)
            {
                pam = new ProductAttributeMapping()
                {
                    ProductId = productId,
                    ProductAttributeId = _deliveryPickupOptionsProductAttributeId,
                    AttributeControlType = AttributeControlType.RadioList,
                };
                await _productAttributeService.InsertProductAttributeMappingAsync(pam);
            }

            return pam;
        }

        private async System.Threading.Tasks.Task AddDeliveryOptionsValuesAsync(
            int pamId,
            AbcDeliveryMap map)
        {
            var values = await _productAttributeService.GetProductAttributeValuesAsync(pamId);

            var deliveryOnlyPav = values.Where(pav => pav.Name.Contains("Home Delivery (")).SingleOrDefault();
            await AddValueAsync(
                pamId,
                deliveryOnlyPav,
                map.DeliveryOnly,
                "Home Delivery ({0}, FREE With Mail-In Rebate)",
                0);

            var deliveryInstallationPav = values.Where(pav => pav.Name.Contains("Home Delivery and Installation (")).SingleOrDefault();
            await AddValueAsync(
                pamId,
                deliveryInstallationPav,
                map.DeliveryInstall,
                "Home Delivery and Installation ({0})",
                10);
        }

        private async System.Threading.Tasks.Task AddValueAsync(
            int pamId,
            ProductAttributeValue pav,
            int itemNumber,
            string displayName,
            int displayOrder)
        {
            if (pav == null && itemNumber != 0)
            {
                var item = await _abcDeliveryService.GetAbcDeliveryItemByItemNumberAsync(itemNumber);
                var priceDisplay = await _priceFormatter.FormatPriceAsync(item.Price);
                pav = new ProductAttributeValue()
                {
                    ProductAttributeMappingId = pamId,
                    Name = string.Format(displayName, priceDisplay),
                    Cost = itemNumber,
                    PriceAdjustment = item.Price,
                    IsPreSelected = displayName.Contains("Home Delivery ("),
                    DisplayOrder = displayOrder,
                };

                await _productAttributeService.InsertProductAttributeValueAsync(pav);
            }
        }
    }
}