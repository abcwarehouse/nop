using Dapper;
using Dapper.Contrib.Extensions;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public class ProductDataProductService : IProductDataProductService
    {
        private readonly ImportSettings _importSettings;

        public ProductDataProductService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }
        
        public bool FindProductDataProduct(string sku, string brand)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                bool result = false;
                var pdpEnum = connection.Query<ProductDataProduct>(
                    //"SELECT * FROM ProductDataProducts WHERE SKU='" + sku + "' AND Brand='" + brand + "' "
                    "SELECT * FROM ProductDataProducts WHERE SKU='" + sku + "' "
                );
                
                if (pdpEnum.Count() > 0)
				{
                    foreach (var pdp in pdpEnum.ToList())
                    {                        
                        connection.Query(string.Format("DELETE FROM [dbo].[ProductDataProductDimensions] WHERE ProductDataProduct_id = {0}", pdp.id));
                        connection.Query(string.Format("DELETE FROM [dbo].[ProductDataProductDownloads] WHERE ProductDataProduct_id = {0}", pdp.id));
                        connection.Query(string.Format("DELETE FROM [dbo].[ProductDataProductFeatures] WHERE ProductDataProduct_id = {0}", pdp.id));
                        connection.Query(string.Format("DELETE FROM [dbo].[ProductDataProductFilters] WHERE ProductDataProduct_id = {0}", pdp.id));
                        connection.Query(string.Format("DELETE FROM [dbo].[ProductDataProductImages] WHERE ProductDataProduct_id = {0}", pdp.id));
                        connection.Query(string.Format("DELETE FROM [dbo].[ProductDataProductpmaps] WHERE ProductDataProduct_id = {0}", pdp.id));
                        connection.Query(string.Format("DELETE FROM [dbo].[ProductDataProductRelatedItems] WHERE ProductDataProduct_id = {0}", pdp.id));
                        connection.Query(string.Format("DELETE FROM [dbo].[ProductDataProducts] WHERE id = {0}", pdp.id));
                    }
                    
                    result = true;
                }                    

                return result;
            }
        }

        public int InsertProductDataProduct(ProductDataProduct pdp)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return (int)connection.Insert(pdp);
            }
        }
    }
}