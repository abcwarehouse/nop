using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using Nop.Plugin.Misc.AbcSync.Domain.Staging.SiteOnTime;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public interface IProductDataProductService
    {
        IEnumerable<ProductDataProduct> GetProductDataProducts();
        int InsertProductDataProduct(ProductDataProduct productDataProduct);
        void InsertProductDataProductDimension(ProductDataProductDimension pdpDimension);
        void InsertProductDataProductDownload(ProductDataProductDownload pdpDownload); 
        void InsertProductDataProductFeature(ProductDataProductFeature pdpFeature); 
        void InsertProductDataProductFilter(ProductDataProductFilter pdpFilter); 
        void InsertProductDataProductImage(ProductDataProductImage pdpImage); 
        void InsertProductDataProductpmap(ProductDataProductpmap pdpPmap); 
        void InsertProductDataProductRelatedItem(ProductDataProductRelatedItem pdpRelatedItem); 
    }
}