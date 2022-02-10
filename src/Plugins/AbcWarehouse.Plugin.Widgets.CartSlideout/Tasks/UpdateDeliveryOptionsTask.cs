using System.Linq;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Domain;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Services;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Tasks;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout
{
    public partial class UpdateDeliveryOptionsTask : IScheduleTask
    {
        private readonly IAbcDeliveryService _abcDeliveryService;
        private readonly ICategoryService _categoryService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeService _productAttributeService;

        private int _deliveryPickupOptionsProductAttributeId;
        private int _haulAwayDeliveryProductAttributeId;
        private int _haulAwayDeliveryInstallProductAttributeId;

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
            await InitializeAsync();

            var abcDeliveryMaps = await _abcDeliveryService.GetAbcDeliveryMapsAsync();
            foreach (var abcDeliveryMap in abcDeliveryMaps)
            {
                var productIds = (await _categoryService.GetProductCategoriesByCategoryIdAsync(abcDeliveryMap.CategoryId)).Select(pc => pc.ProductId);
                foreach (var productId in productIds)
                {
                    (int deliveryOptionsPamId, int? deliveryPavId, int? deliveryInstallPavId) = await AddDeliveryOptionsAsync(productId, abcDeliveryMap);
                    await AddHaulAwayAsync(
                        productId,
                        abcDeliveryMap,
                        deliveryOptionsPamId,
                        deliveryPavId,
                        deliveryInstallPavId);
                }
            }
        }

        private async System.Threading.Tasks.Task<ProductAttributeValue> AddValueAsync(
            int pamId,
            ProductAttributeValue pav,
            int itemNumber,
            string displayName,
            int displayOrder,
            bool isPreSelected)
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
                    IsPreSelected = isPreSelected,
                    DisplayOrder = displayOrder,
                };

                await _productAttributeService.InsertProductAttributeValueAsync(pav);
            }

            return pav;
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            var productAttributes = await _productAttributeService.GetAllProductAttributesAsync();

            _deliveryPickupOptionsProductAttributeId = productAttributes
                .Where(p => p.Name == CartSlideoutConsts.DeliveryPickupOptions)
                .Select(p => p.Id)
                .Single();

            _haulAwayDeliveryProductAttributeId = productAttributes
                .Where(p => p.Name == CartSlideoutConsts.HaulAwayDelivery)
                .Select(p => p.Id)
                .Single();

            _haulAwayDeliveryInstallProductAttributeId = productAttributes
                .Where(p => p.Name == CartSlideoutConsts.HaulAwayDeliveryInstall)
                .Select(p => p.Id)
                .Single();
        }
    }
}