using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcCore.Services;

namespace Nop.Plugin.Misc.AbcSync
{
    public class UpdateMetaTagsTask : IScheduleTask
    {
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IProductService _productService;

        private readonly ImportSettings _importSettings;

        public UpdateMetaTagsTask(
            IGenericAttributeService genericAttributeService,
            IProductAbcDescriptionService productAbcDescriptionService,
            IProductService productService,
            ImportSettings importSettings)
        {
            _genericAttributeService = genericAttributeService;
            _productAbcDescriptionService = productAbcDescriptionService;
            _productService = productService;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipUpdateMetaTagsTask)
            {
                this.Skipped();
                return;
            }

            var products = await _productService.SearchProductsAsync();
            var productsWithoutMetaTags = products.Where(p => string.IsNullOrWhiteSpace(p.MetaTitle));

            foreach (var product in productsWithoutMetaTags)
            {
                await UpdateProductMetaTagsAsync(product);
            }
        }

        private async System.Threading.Tasks.Task UpdateProductMetaTagsAsync(Product product)
        {
            var description = await GetProductDescriptionAsync(product);
            if (description == null) { return; }
            // clean up
            var escapeIndex = description.IndexOf("\r\n");
            if (escapeIndex > -1)
            {
                description = description.Substring(0, description.IndexOf("\r\n"));
            }

            var productNameParts = product.Name.Split(" ");
            var model = productNameParts.Last();
            var brand = string.Join(" ", productNameParts.Take(productNameParts.Length - 1));

            product.MetaTitle = $"{brand} {description} {model}";
            product.MetaDescription = description;
            await _productService.UpdateProductAsync(product);
        }

        // This is copied - maybe we put this in a service?
        private async System.Threading.Tasks.Task<string> GetProductDescriptionAsync(Product product)
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