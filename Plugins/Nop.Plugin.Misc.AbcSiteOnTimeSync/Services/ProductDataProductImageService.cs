using Dapper;
using Dapper.Contrib.Extensions;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public class ProductDataProductImageService : IProductDataProductImageService
    {
        private readonly ImportSettings _importSettings;

        public ProductDataProductImageService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }
        
        public void InsertProductDataProductImages(List<ProductDataProductImage> images, int pdpId)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                foreach(var i in images)
				{
                    i.ProductDataProduct_id = pdpId;
                    connection.Insert(i);
                }                
            }
        }
    }
}