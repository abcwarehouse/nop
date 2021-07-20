using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public interface IProductDataProductFilterService
    {

        void InsertProductDataProductFilters(List<ProductDataProductFilter> filters, int pdpId);
    }
}