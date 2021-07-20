using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public interface IProductDataProductService
    {
        bool FindProductDataProduct(string sku, string brand);
        int InsertProductDataProduct(ProductDataProduct productDataProduct);
    }
}