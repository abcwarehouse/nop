using Dapper;
using Dapper.Contrib.Extensions;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public class ProductDataProductFeatureService : IProductDataProductFeatureService
    {
        private readonly ImportSettings _importSettings;

        public ProductDataProductFeatureService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public void InsertProductDataProductFeatures(List<ProductDataProductFeature> features, int pdpId)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                foreach (var f in features)
                {
                    f.ProductDataProduct_id = pdpId;
                    connection.Insert(f);
                }
            }
        }
    }
}