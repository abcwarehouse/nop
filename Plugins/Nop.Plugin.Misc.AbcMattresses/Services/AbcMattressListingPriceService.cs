using Nop.Services.Catalog;
using System.Linq;
using System;
using Nop.Core;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressListingPriceService : IAbcMattressListingPriceService
    {
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IWebHelper _webHelper;

        public AbcMattressListingPriceService(
            IProductService productService,
            IProductAttributeService productAttributeService,
            IWebHelper webHelper
        )
        {
            _productService = productService;
            _productAttributeService = productAttributeService;
            _webHelper = webHelper;
        }

        public decimal? GetListingPriceForMattressProduct(
            int productId,
            string categorySlug
        )
        {
            // only need to do this if we're on the 'shop by size' categories
            // but we're opening this up to be called anywhere
            // including the JSON schema for google crawler
            var url = _webHelper.GetThisPageUrl(true);
            if (!IsSizeCategoryPage(url)) { return null; }

            var mattressSizePam = _productAttributeService.GetProductAttributeMappingsByProductId(productId)
                                               .Where(pam =>
                                                   _productAttributeService.GetProductAttributeById(
                                                       pam.ProductAttributeId
                                                    )?.Name == AbcMattressesConsts.MattressSizeName
                                               )
                                               .FirstOrDefault();
            if (mattressSizePam == null) // if no mattress sizes, return price
            {
                return null;
            }

            var value = _productAttributeService.GetProductAttributeValues(
                mattressSizePam.Id
            ).Where(pav => pav.Name == GetMattressSizeFromUrl(categorySlug))
             .FirstOrDefault();
            if (value == null) // no matching sizes, check for default (queen)
            {
                value = _productAttributeService.GetProductAttributeValues(
                    mattressSizePam.Id
                ).Where(pav => pav.Name == AbcMattressesConsts.Queen)
                .FirstOrDefault();
            }

            var product = _productService.GetProductById(productId);
            return value == null ? (decimal?)null :
                                    Math.Round(product.Price + value.PriceAdjustment, 2);
        }

        private bool IsSizeCategoryPage(string url)
        {
            return url.Contains("twin-mattress") ||
                   url.Contains("twinxl-mattress") ||
                   url.Contains("full-mattress") ||
                   url.Contains("queen-mattress") ||
                   url.Contains("king-mattress") ||
                   url.Contains("california-king-mattress");
        }

        // default to queen if nothing matches
        private string GetMattressSizeFromUrl(string url)
        {
            var slug = url.Substring(url.LastIndexOf('/') + 1);
            switch (slug)
            {
                case "california-king-mattress":
                    return AbcMattressesConsts.CaliforniaKing;
                case "king-mattress":
                    return AbcMattressesConsts.King;
                case "full-mattress":
                    return AbcMattressesConsts.Full;
                case "twin-extra-long-mattress":
                    return AbcMattressesConsts.TwinXL;
                case "twin-mattress":
                    return AbcMattressesConsts.Twin;
                default:
                    return AbcMattressesConsts.CaliforniaKing;

            }
        }
    }
}
