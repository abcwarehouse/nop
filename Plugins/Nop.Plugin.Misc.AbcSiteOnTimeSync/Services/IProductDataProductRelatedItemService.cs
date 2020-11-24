using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public interface IProductDataProductRelatedItemService
    {

        void InsertProductDataProductRelatedItems(List<ProductDataProductRelatedItem> relatedItems, int pdpId);
    }
}