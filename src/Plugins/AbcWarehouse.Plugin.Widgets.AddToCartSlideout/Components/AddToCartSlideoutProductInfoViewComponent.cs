using System.Linq;
using System.Threading.Tasks;
using AbcWarehouse.Plugin.Widgets.AddToCartSlideout.Models;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Media;
using Nop.Web.Framework.Components;

namespace AbcWarehouse.Plugin.Widgets.AddToCartSlideout.Components
{
    public class AddToCartSlideoutProductInfoViewComponent : NopViewComponent
    {
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IPictureService _pictureService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IProductService _productService;

        public AddToCartSlideoutProductInfoViewComponent(
            IGenericAttributeService genericAttributeService,
            IPictureService pictureService,
            IProductAbcDescriptionService productAbcDescriptionService,
            IProductService productService)
        {
            _genericAttributeService = genericAttributeService;
            _pictureService = pictureService;
            _productAbcDescriptionService = productAbcDescriptionService;
            _productService = productService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            var productName = product.Name;
            var productPicture = (await _productService.GetProductPicturesByProductIdAsync(product.Id)).FirstOrDefault();
            var pictureUrl = productPicture != null ?
                await _pictureService.GetPictureUrlAsync(productPicture.PictureId) :
                string.Empty;

            var model = new ProductInfoModel()
            {
                ImageUrl = pictureUrl,
                Name = productName,
                Description = await GetProductDescriptionAsync(product),
            };

            return View("~/Plugins/Widgets.AddToCartSlideout/Views/_ProductInfo.cshtml", model);
        }

        private async Task<string> GetProductDescriptionAsync(Product product)
        {
            var plpDescription = await _genericAttributeService.GetAttributeAsync<Product, string>(
                product.Id, "PLPDescription");

            if (plpDescription != null)
            {
                return plpDescription;
            }

            var pad = await _productAbcDescriptionService.GetProductAbcDescriptionByProductIdAsync(product.Id);
            return pad != null ? pad.AbcDescription : product.ShortDescription;
        }
    }
}
