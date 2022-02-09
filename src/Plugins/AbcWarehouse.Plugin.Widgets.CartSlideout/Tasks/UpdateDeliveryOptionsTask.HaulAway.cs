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
            AbcDeliveryMap map)
        {
            var haulAwayDeliveryPam = await AddHaulAwayAttributeAsync(productId, _haulAwayDeliveryProductAttributeId);
            var haulAwayDeliveryInstallPam = await AddHaulAwayAttributeAsync(productId, _haulAwayDeliveryInstallProductAttributeId);
        }

        private async System.Threading.Tasks.Task<ProductAttributeMapping> AddHaulAwayAttributeAsync(int productId, int productAttributeId)
        {
            var pam = (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId))
                                                    .SingleOrDefault(pam => pam.ProductAttributeId == productAttributeId);
            if (pam == null)
            {
                pam = new ProductAttributeMapping()
                {
                    ProductId = productId,
                    ProductAttributeId = productAttributeId,
                    AttributeControlType = AttributeControlType.Checkboxes,
                };
                await _productAttributeService.InsertProductAttributeMappingAsync(pam);
            }

            return pam;
        }
    }
}