using Dapper;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using Nop.Plugin.Misc.AbcSync.Domain.Staging.SiteOnTime;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
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

        public IEnumerable<ProductDataProductImage> GetProductDataProductImages()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<ProductDataProductImage>(
                    "SELECT * FROM ProductDataProductImages"
                );
            }
        }
    }
}