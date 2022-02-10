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
            int? deliveryInstallPavId)
        {
            var haulAwayDeliveryPam = await AddHaulAwayAttributeAsync(
                productId,
                _haulAwayDeliveryProductAttributeId,
                deliveryOptionsPamId,
                deliveryPavId);

            // add value

            var haulAwayDeliveryInstallPam = await AddHaulAwayAttributeAsync(
                productId,
                _haulAwayDeliveryInstallProductAttributeId,
                deliveryOptionsPamId,
                deliveryInstallPavId);

            // add value
        }

        private async System.Threading.Tasks.Task<ProductAttributeMapping> AddHaulAwayAttributeAsync(
            int productId,
            int productAttributeId,
            int deliveryOptionsPamId,
            int? pavId)
        {
            if (pavId == null) { return null; }

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
    }
}