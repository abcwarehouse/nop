using Nop.Services.Catalog;
using System.Linq;
using System;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressListingPriceService : IAbcMattressListingPriceService
    {
        private readonly IProductAttributeService _productAttributeService;

        public AbcMattressListingPriceService(
            IProductAttributeService productAttributeService
        )
        {
            _productAttributeService = productAttributeService;
        }

        public decimal? GetListingPriceForMattressProduct(
            int productId,
            string categorySlug
        )
        {
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

            return value == null ? (decimal?)null :
                                    Math.Round(value.PriceAdjustment, 2);
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
