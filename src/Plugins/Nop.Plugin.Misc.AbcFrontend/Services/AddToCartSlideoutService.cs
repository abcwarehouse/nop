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
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IProductService _productService;

        public AddToCartSlideoutService(
            IPictureService pictureService,
            IProductAbcDescriptionService productAbcDescriptionService,
            IProductService productService
        ) {
            _pictureService = pictureService;
            _productAbcDescriptionService = productAbcDescriptionService;
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

            return new AddToCartSlideoutInfo()
            {
                ProductName = productName,
                ProductDescription = productDescription,
                ProductPictureUrl = pictureUrl
            };
        }
    }
}