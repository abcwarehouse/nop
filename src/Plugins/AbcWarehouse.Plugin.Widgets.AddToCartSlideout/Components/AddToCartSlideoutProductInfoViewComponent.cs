using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Web.Framework.Components;
using System.Threading.Tasks;

namespace AbcWarehouse.Plugin.Widgets.AddToCartSlideout.Components
{
    public class AddToCartSlideoutProductInfoViewComponent : NopViewComponent
    {
        private readonly IProductService _productService;
        
        public AddToCartSlideoutProductInfoViewComponent(
            IProductService productService
        ) {
            _productService = productService;
        }

        public IViewComponentResult Invoke(string widgetZone, int productId)
        {
            var product = _productService.GetProductById(productId);
            var productName = product.Name;
            var productPicture = (await _productService.GetProductPicturesByProductIdAsync(product.Id)).FirstOrDefault();
            var pictureUrl = productPicture != null ?
                await _pictureService.GetPictureUrlAsync(productPicture.PictureId) :
                "";

            var model = new ProductInfoModel()
            {
                ImageUrl = pictureUrl,
                Name = productName,
                Description = GetProductDescription(product)
            };

            return View("~/Plugins/Widgets.AddToCartSlideout/Views/_ProductInfo.cshtml", model);
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
