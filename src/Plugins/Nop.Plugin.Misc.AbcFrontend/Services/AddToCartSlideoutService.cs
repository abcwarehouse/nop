using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcFrontend.Models;
using Nop.Services.Catalog;
using Nop.Services.Media;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public class AddToCartSlideoutService : IAddToCartSlideoutService
    {
        private readonly IPictureService _pictureService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;

        public AddToCartSlideoutService(
            IPictureService pictureService,
            IPriceFormatter priceFormatter,
            IProductAbcDescriptionService productAbcDescriptionService,
            IProductAttributeService productAttributeService,
            IProductService productService
        ) {
            _pictureService = pictureService;
            _priceFormatter = priceFormatter;
            _productAbcDescriptionService = productAbcDescriptionService;
            _productAttributeService = productAttributeService;
            _productService = productService;
        }

        public async Task<AddToCartSlideoutInfo> GetAddToCartSlideoutInfoAsync(Product product)
        {
            var productName = product.Name;
            var pad = await _productAbcDescriptionService.GetProductAbcDescriptionByProductIdAsync(product.Id);
            var productDescription = pad != null ? pad.AbcDescription : product.ShortDescription;
            var productPicture = (await _productService.GetProductPicturesByProductIdAsync(product.Id)).FirstOrDefault();
            var pictureUrl = productPicture != null ?
                await _pictureService.GetPictureUrlAsync(productPicture.PictureId) :
                "";

            var productAttributeMappings = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
            var productAttributes = await productAttributeMappings.SelectAwait(async pam => await _productAttributeService.GetProductAttributeByIdAsync(pam.ProductAttributeId))
                                                                  .ToListAsync();
            var isAbcDeliveryItem = productAttributes.Any(pa => pa.Name == "Home Delivery");

            var subtotal = await _priceFormatter.FormatPriceAsync(product.Price);

            return new AddToCartSlideoutInfo()
            {
                ProductName = productName,
                ProductDescription = productDescription,
                ProductPictureUrl = pictureUrl,
                IsAbcDeliveryItem = isAbcDeliveryItem,
                Subtotal = subtotal
            };
        }
    }
}