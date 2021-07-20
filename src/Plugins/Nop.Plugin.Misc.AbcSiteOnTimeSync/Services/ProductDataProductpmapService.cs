using Dapper;
using Dapper.Contrib.Extensions;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public class ProductDataProductpmapService : IProductDataProductpmapService
    {
        private readonly ImportSettings _importSettings;

        public ProductDataProductpmapService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public void InsertProductDataProductpmaps(List<ProductDataProductpmap> pmaps, int pdpId)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                foreach (var p in pmaps)
                {
                    p.ProductDataProduct_id = pdpId;
                    connection.Insert(p);
                }
            }
        }
    }
}