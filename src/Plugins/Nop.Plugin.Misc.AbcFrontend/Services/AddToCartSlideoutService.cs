using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcFrontend.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Media;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public class AddToCartSlideoutService : IAddToCartSlideoutService
    {
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IPictureService _pictureService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;

        public AddToCartSlideoutService(
            IGenericAttributeService genericAttributeService,
            IPictureService pictureService,
            IProductAbcDescriptionService productAbcDescriptionService,
            IProductAttributeService productAttributeService,
            IProductService productService
        ) {
            _genericAttributeService = genericAttributeService;
            _pictureService = pictureService;
            _productAbcDescriptionService = productAbcDescriptionService;
            _productAttributeService = productAttributeService;
            _productService = productService;
        }

        public async Task<AddToCartSlideoutInfo> GetAddToCartSlideoutInfoAsync(Product product)
        {
            var productAttributeMappings = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
            var productAttributes = await productAttributeMappings.SelectAwait(async pam => await _productAttributeService.GetProductAttributeByIdAsync(pam.ProductAttributeId))
                                                                  .ToListAsync();
            var isAbcDeliveryItem = productAttributes.Any(pa => pa.Name == "Home Delivery");

            return new AddToCartSlideoutInfo()
            {
                IsAbcDeliveryItem = isAbcDeliveryItem,
                Subtotal = product.Price
            };
        }

        private async Task<string> GetProductDescription(Product product)
        {
            var plpDescription = await _genericAttributeService.GetAttributeAsync<Product, string>(
                product.Id, "PLPDescription"
            );

            if (plpDescription != null) { return plpDescription; }
            var pad = await _productAbcDescriptionService.GetProductAbcDescriptionByProductIdAsync(product.Id);
            return pad != null ? pad.AbcDescription : product.ShortDescription;
        }
    }
}