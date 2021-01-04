using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressListingPriceService
    {
        decimal? GetListingPriceForMattressProduct(
            int productId,
            string categorySlug
        );
    }
}
