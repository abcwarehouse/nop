using Dapper;
using Dapper.Contrib.Extensions;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public class ProductDataProductDimensionService : IProductDataProductDimensionService
    {
        private readonly ImportSettings _importSettings;

        public ProductDataProductDimensionService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public void InsertProductDataProductDimensions(List<ProductDataProductDimension> dimensions, int pdpId)
		{
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                foreach(var d in dimensions)
				{
                    d.ProductDataProduct_id = pdpId;
                    connection.Insert(d);
				}
            }
        }
    }
}