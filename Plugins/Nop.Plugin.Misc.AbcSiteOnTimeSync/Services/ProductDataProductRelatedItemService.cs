using Dapper;
using Dapper.Contrib.Extensions;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public class ProductDataProductRelatedItemService : IProductDataProductRelatedItemService
    {
        private readonly ImportSettings _importSettings;

        public ProductDataProductRelatedItemService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public void InsertProductDataProductRelatedItems(List<ProductDataProductRelatedItem> relatedItems, int pdpId)
		{
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                foreach(var r in relatedItems)
				{
                    r.ProductDataProduct_id = pdpId;
                    connection.Insert(r);
				}
            }
        }
    }
}