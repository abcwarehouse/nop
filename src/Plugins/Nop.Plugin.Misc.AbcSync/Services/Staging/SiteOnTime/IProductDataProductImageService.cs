using Nop.Plugin.Misc.AbcSync.Domain.Staging.SiteOnTime;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public interface IProductDataProductImageService
    {
        IEnumerable<ProductDataProductImage> GetProductDataProductImages();
    }
}