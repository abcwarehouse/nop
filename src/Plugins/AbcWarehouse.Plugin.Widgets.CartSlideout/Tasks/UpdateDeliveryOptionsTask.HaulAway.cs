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
                    _haulAwayDeliveryProductAttributeId,
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
                    _haulAwayDeliveryInstallProductAttributeId,
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
                _haulAwayDeliveryProductAttributeId,
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
    }
}