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
        private async System.Threading.Tasks.Task AddDeliveryOptionsAsync(
            int productId,
            AbcDeliveryMap map)
        {
            var pam = await AddDeliveryOptionsAttributeAsync(productId);
            var values = await _productAttributeService.GetProductAttributeValuesAsync(pam.Id);

            var deliveryOnlyPav = values.Where(pav => pav.Name.Contains("Home Delivery (")).SingleOrDefault();
            await AddValueAsync(
                pam.Id,
                deliveryOnlyPav,
                map.DeliveryOnly,
                "Home Delivery ({0}, FREE With Mail-In Rebate)",
                0);

            var deliveryInstallationPav = values.Where(pav => pav.Name.Contains("Home Delivery and Installation (")).SingleOrDefault();
            await AddValueAsync(
                pam.Id,
                deliveryInstallationPav,
                map.DeliveryInstall,
                "Home Delivery and Installation ({0})",
                10);
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
    }
}