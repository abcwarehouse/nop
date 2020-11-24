using Dapper;
using Dapper.Contrib.Extensions;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public class ProductDataProductFilterService : IProductDataProductFilterService
    {
        private readonly ImportSettings _importSettings;

        public ProductDataProductFilterService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public void InsertProductDataProductFilters(List<ProductDataProductFilter> filters, int pdpId)
		{
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                foreach(var f in filters)
				{
                    f.ProductDataProduct_id = pdpId;
                    connection.Insert(f);
				}
            }
        }
    }
}