using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public interface IProductDataProductpmapService
    {

        void InsertProductDataProductpmaps(List<ProductDataProductpmap> pmaps, int pdpId);
    }
}