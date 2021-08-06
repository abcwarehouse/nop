using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public interface IProductDataProductImageService
    {
        void InsertProductDataProductImages(List<ProductDataProductImage> images, int pdpId);
    }
}